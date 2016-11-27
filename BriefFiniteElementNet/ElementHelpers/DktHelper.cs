﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BriefFiniteElementNet.Elements;
using BriefFiniteElementNet.Integration;

namespace BriefFiniteElementNet.ElementHelpers
{
    public class DktHelper:IElementHelper
    {
        /// <inheritdoc/>
        public Matrix GetBMatrixAt(Element targetElement, Matrix transformMatrix, params double[] isoCoords)
        {
            var tri = targetElement as TriangleElement;

            if (tri == null)
                throw new Exception();

            var xi = isoCoords[0];
            var eta = isoCoords[1];

            #region inits

            var p1g = tri.Nodes[0].Location;
            var p2g = tri.Nodes[1].Location;
            var p3g = tri.Nodes[2].Location;

            var p1l = p1g.TransformBack(transformMatrix);
            var p2l = p2g.TransformBack(transformMatrix);
            var p3l = p3g.TransformBack(transformMatrix);

            var x1 = p1l.X;
            var x2 = p2l.X;
            var x3 = p3l.X;

            var y1 = p1l.Y;
            var y2 = p2l.Y;
            var y3 = p3l.Y;

            var x23 = x2 - x3;
            var x31 = x3 - x1;
            var x12 = x1 - x2;

            var y23 = y2 - y3;
            var y31 = y3 - y1;
            var y12 = y1 - y2;

            var a = 0.5*Math.Abs(x31*y12 - x12*y31);

            var l23_2 = y23*y23 + x23*x23;
            var l31_2 = y31*y31 + x31*x31;
            var l12_2 = y12*y12 + x12*x12;

            var P4 = -6*x23/l23_2;
            var P5 = -6*x31/l31_2;
            var P6 = -6*x12/l12_2;

            var q4 = 3*x23*y23/l23_2;
            var q5 = 3*x31*y31/l31_2;
            var q6 = 3*x12*y12/l12_2;

            var r4 = 3*y23*y23/l23_2;
            var r5 = 3*y31*y31/l31_2;
            var r6 = 3*y12*y12/l12_2;

            var t4 = -6*y23/l23_2;
            var t5 = -6*y31/l31_2;
            var t6 = -6*y12/l12_2;

            #endregion

            #region h{x,y}{kesi,no}

            var hx_xi = new double[]//eq. 4.27 ref [1], also noted in several other references
            {
                P6*(1 - 2*xi) + (P5 - P6)*eta,
                q6*(1 - 2*xi) - (q5 + q6)*eta,
                -4 + 6*(xi + eta) + r6*(1 - 2*xi) - eta*(r5 + r6),
                -P6*(1 - 2*xi) + eta*(P4 + P6),
                q6*(1 - 2*xi) - eta*(q6 - q4),
                -2 + 6*xi + r6*(1 - 2*xi) + eta*(r4 - r6),
                -eta*(P5 + P4),
                eta*(q4 - q5),
                -eta*(r5 - r4)
            };

            var hy_xi = new double[] //eq. 4.28 ref [1], also noted in several other references
            {
                t6*(1 - 2*xi) + eta*(t5 - t6),
                1 + r6*(1 - 2*xi) - eta*(r5 + r6),
                -q6*(1 - 2*xi) + eta*(q5 + q6),
                -t6*(1 - 2*xi) + eta*(t4 + t6),
                -1 + r6*(1 - 2*xi) + eta*(r4 - r6),
                -q6*(1 - 2*xi) - eta*(q4 - q6),
                -eta*(t4 + t5),
                eta*(r4 - r5),
                -eta*(q4 - q5)
            };


            var hx_eta = new double[] //eq. 4.29 ref [1], also noted in several other references
            {
                -P5*(1 - 2*eta) - xi*(P6 - P5),
                q5*(1 - 2*eta) - xi*(q5 + q6),
                -4 + 6*(xi + eta) + r5*(1 - 2*eta) - xi*(r5 + r6),
                xi*(P4 + P6),
                xi*(q4 - q6),
                -xi*(r6 - r4),
                P5*(1 - 2*eta) - xi*(P4 + P5),
                q5*(1 - 2*eta) + xi*(q4 - q5),
                -2 + 6*eta + r5*(1 - 2*eta) + xi*(r4 - r5)
            };

            var hy_eta = new double[] //eq. 4.30 ref [1], also noted in several other references
            {
                -t5*(1 - 2*eta) - xi*(t6 - t5),
                1 + r5*(1 - 2*eta) - xi*(r5 + r6),
                -q5*(1 - 2*eta) + xi*(q5 + q6),
                xi*(t4 + t6),
                xi*(r4 - r6),
                -xi*(q4 - q6),
                t5*(1 - 2*eta) - xi*(t4 + t5),
                -1 + r5*(1 - 2*eta) + xi*(r4 - r5),
                -q5*(1 - 2*eta) - xi*(q4 - q5)
            };
            
            #endregion

            var buf = new Matrix(3, 9);

            for (var i = 0; i < 9; i++)
            {
                buf[0, i] = y31 * hx_xi[i] + y12 * hx_eta[i];
                buf[1, i] = -x31 * hy_xi[i] - x12 * hy_eta[i];
                buf[2, i] = -x31*hx_xi[i] - x12*hx_eta[i] + y31*hy_xi[i] + y12*hy_eta[i];
            }//eq. 4.26 page 46 ref [1]

            buf.MultiplyByConstant(1 / (2 * a));

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetDMatrixAt(Element targetElement, Matrix transformMatrix, params double[] isoCoords)
        {
            var tri = targetElement as TriangleElement;

            if (tri == null)
                throw new Exception();

            var mat = tri._material.GetMaterialPropertiesAt(isoCoords).Matterial;
            var t = tri.Section.GetThicknessAt(isoCoords);

            var d = new Matrix(3, 3);

            {
                var cf = t*t*t/12;

                d[0, 0] = mat.Ex / (1 - mat.NuXy * mat.NuYx);
                d[1, 1] = mat.Ey / (1 - mat.NuXy * mat.NuYx);
                d[0, 1] = d[1, 0] =
                    mat.Ex*mat.NuYx/(1 - mat.NuXy*mat.NuYx);
                //or mat.Ey * mat.NuXy / (1 - mat.NuXy * mat.NuYx);

                d[2, 2] = mat.Ex/(2*(1 + mat.NuXy));

                //p55 http://www.code-aster.org/doc/v11/en/man_r/r3/r3.07.03.pdf

                d.MultiplyByConstant(cf);
            }

            return d;
        }

        public Matrix GetRhoMatrixAt(Element targetElement, Matrix transformMatrix, params double[] isoCoords)
        {
            throw new NotImplementedException();
        }

        public Matrix GetMuMatrixAt(Element targetElement, Matrix transformMatrix, params double[] isoCoords)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Matrix GetNMatrixAt(Element targetElement, Matrix transformMatrix, params double[] isoCoords)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Matrix GetJMatrixAt(Element targetElement, Matrix transformMatrix, params double[] isoCoords)
        {
            var tri = targetElement as TriangleElement;

            if (tri == null)
                throw new Exception();

            var xi = isoCoords[0];
            var eta = isoCoords[1];

            var p1g = tri.Nodes[0].Location;
            var p2g = tri.Nodes[1].Location;
            var p3g = tri.Nodes[2].Location;

            var p1l = p1g.TransformBack(transformMatrix);
            var p2l = p2g.TransformBack(transformMatrix);
            var p3l = p3g.TransformBack(transformMatrix);

            var x1 = p1l.X;
            var x2 = p2l.X;
            var x3 = p3l.X;

            var y1 = p1l.Y;
            var y2 = p2l.Y;
            var y3 = p3l.Y;

            var buf = new Matrix(2, 2);

            var x12 = x1 - x2;
            var x31 = x3 - x1;
            var y31 = y3 - y1;

            var y23 = y2 - y3;

            var y13 = y1 - y3;
            var y12 = y1 - y2;
            var X12 = x1 - x2;

            var x23 = x2 - x3;

            buf[0, 0] = x31;
            buf[1, 1] = y12;

            buf[0, 1] = x12;
            buf[1, 0] = y31;

            return buf;
        }

        /// <inheritdoc/>
        public Matrix CalcLocalKMatrix(Element targetElement, Matrix transformMatrix)
        {
            var intg = new BriefFiniteElementNet.Integration.GaussianIntegrator();

            intg.A2 = 1;
            intg.A1 = 0;

            intg.F2 = (gama => 1);
            intg.F1 = (gama => 0);

            intg.G2 = ((eta, gama) => 1 - eta);
            intg.G1 = ((eta, gama) => 0);

            intg.XiPointCount = intg.EtaPointCount = 3;
            intg.GammaPointCount = 1;

            intg.H = new FunctionMatrixFunction((xi, eta, gamma) =>
            {
                var b = GetBMatrixAt(targetElement, transformMatrix, xi, eta);

                var d = this.GetDMatrixAt(targetElement, transformMatrix, xi, eta);

                var j = GetJMatrixAt(targetElement, transformMatrix, xi, eta);

                var detJ = j.Determinant();

                var ki = b.Transpose() * d * b;

                ki.MultiplyByConstant(Math.Abs(j.Determinant()));

                return ki;
            });

            var res = intg.Integrate();

            return res;
        }

        public Matrix CalcLocalMMatrix(Element targetElement, Matrix transformMatrix)
        {
            throw new NotImplementedException();
        }

        public Matrix CalcLocalCMatrix(Element targetElement, Matrix transformMatrix)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public FluentElementPermuteManager.ElementLocalDof[] GetDofOrder(Element targetElement)
        {
            var buf = new FluentElementPermuteManager.ElementLocalDof[]
            {
                new FluentElementPermuteManager.ElementLocalDof(0, DoF.Dz),
                new FluentElementPermuteManager.ElementLocalDof(0, DoF.Rx),
                new FluentElementPermuteManager.ElementLocalDof(0, DoF.Ry),

                new FluentElementPermuteManager.ElementLocalDof(1, DoF.Dz),
                new FluentElementPermuteManager.ElementLocalDof(1, DoF.Rx),
                new FluentElementPermuteManager.ElementLocalDof(1, DoF.Ry),

                new FluentElementPermuteManager.ElementLocalDof(2, DoF.Dz),
                new FluentElementPermuteManager.ElementLocalDof(2, DoF.Rx),
                new FluentElementPermuteManager.ElementLocalDof(2, DoF.Ry),
            };

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetLocalInternalForceAt(Element targetElement, Matrix transformMatrix, Displacement[] globalDisplacements,
            params double[] isoCoords)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public int GetNMaxOrder(Element targetElement, Matrix transformMatrix)
        {
            throw new NotImplementedException();
        }

        public int GetBMaxOrder(Element targetElement, Matrix transformMatrix)
        {
            throw new NotImplementedException();
        }

        public int GetDetJOrder(Element targetElement, Matrix transformMatrix)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Displacement GetLocalDisplacementAt(Element targetElement, Matrix transformMatrix, Displacement[] localDisplacements,
            params double[] isoCoords)
        {
            throw new NotImplementedException();
        }
    }
}
