using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panel.Select
{
    internal class Dimensions
    {
        public int Width { get; }
        public int Height { get; }

        public Dimensions(int width, int height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentOutOfRangeException("Dimensions cannot be negative.");
            }

            Width = width;
            Height = height;
        }
    }
}
