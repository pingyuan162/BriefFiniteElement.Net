﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BriefFiniteElementNet.Elements
{
    /// <summary>
    /// Represents the mechanical properties of triangle element in specific point
    /// </summary>
    public class TriangleCoordinatedMechanicalProperties
    {
        private OrthotropicMaterial mat;

        public OrthotropicMaterial Matterial
        {
            get { return mat; }
            set { mat = value; }
        }
    }
}
