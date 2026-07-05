using System;
using System.Collections.Generic;

namespace WeaponMazeAlchemy.Prototype
{
    public class FloorTemplate
    {
        private readonly string[] rows;

        public FloorTemplate(string name, params string[] rows)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Template name is required.", nameof(name));
            }

            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException("Template rows are required.", nameof(rows));
            }

            int width = rows[0]?.Length ?? 0;
            if (width == 0)
            {
                throw new ArgumentException("Template rows must not be empty.", nameof(rows));
            }

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || rows[i].Length != width)
                {
                    throw new ArgumentException("All template rows must have the same width.", nameof(rows));
                }
            }

            Name = name;
            this.rows = rows;
            Width = width;
            Height = rows.Length;
        }

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<string> Rows => rows;
    }
}
