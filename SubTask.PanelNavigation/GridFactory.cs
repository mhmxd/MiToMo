using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Common.Constants;
using static Common.Helpers.Tools;
using static Common.Settings.ExpLayouts;

namespace SubTask.PanelNavigation
{
    internal class GridFactory
    {
        #region Constants & Dimensions
        private static readonly double ROW_HEIGHT = ButtonFactory.GetButtonHeight();

        // Grid Unit Helpers
        private static double GridMM(double units) => units * MM2PX(Config.GRID_UNIT_MM);

        private static readonly double GUTTER_4PX = GridMM(1);
        private static readonly double GUTTER_8PX = GridMM(2);
        private static readonly double GUTTER_12PX = GridMM(3);
        private static readonly double GUTTER_20PX = GridMM(5);
        #endregion

        #region Public Grid Entries (Simple, Moderate, Complex)

        public static Grid CreateSimpleTopGrid() => BuildManifestGrid(new[] { SimpleTopRow }, ROW_HEIGHT, GUTTER_8PX);

        public static Grid CreateModerateTopGrid() => BuildManifestGrid(new[] { ModerateTopRow1, ModerateTopRow2 }, ROW_HEIGHT, GUTTER_8PX);

        public static Grid CreateModerateSideGrid()
        {
            var grid = new Grid { UseLayoutRounding = true };
            int colInd = -1;

            // Custom vertical columns
            AddGroupWithGutter(grid, CreateVerticalManifestStack(ModerateSideCol1, GUTTER_12PX), ref colInd, GUTTER_12PX);
            AddGroupWithGutter(grid, CreateVerticalManifestStack(ModerateSideCol2, GUTTER_12PX), ref colInd);

            return grid;
        }

        public static Grid CreateComplexTopGrid()
        {
            var grid = new Grid { UseLayoutRounding = true };
            int colInd = -1;

            var creators = new List<Func<FrameworkElement>> {
            CreateComplexTopGroup1, CreateComplexTopGroup2, CreateComplexTopGroup3,
            CreateComplexTopGroup4, CreateComplexTopGroup5, CreateComplexTopGroup6,
            CreateComplexTopGroup7, CreateComplexTopGroup8, CreateComplexTopGroup9
        };

            for (int i = 0; i < creators.Count; i++)
                AddGroupWithGutter(grid, creators[i](), ref colInd, (i == creators.Count - 1) ? -1 : GUTTER_20PX);

            return grid;
        }

        public static Grid CreateComplexSideGrid()
        {
            var grid = new Grid { UseLayoutRounding = true };
            int rowInd = -1;

            var sideCreators = new List<Func<FrameworkElement>> {
            CreateComplexSideGroup1, CreateComplexSideGroup2, CreateComplexSideGroup3,
            CreateComplexSideGroup4, CreateComplexSideGroup5, CreateComplexSideGroup6
        };

            for (int i = 0; i < sideCreators.Count; i++)
                AddRowGroupWithGutter(grid, sideCreators[i](), ref rowInd, (i == sideCreators.Count - 1) ? -1 : GUTTER_20PX);

            return grid;
        }
        #endregion

        #region Complex Group Creators (Logic-Driven)

        private static Grid CreateComplexSideGroup1() => BuildManifestGrid(new[] { ComplexSideGroup1Row1, ComplexSideGroup1Row2, ComplexSideGroup1Row3 }, ROW_HEIGHT, GUTTER_4PX);
        private static Grid CreateComplexSideGroup2() => BuildManifestGrid(new[] { ComplexSideGroup2Row1, ComplexSideGroup2Row2 }, ROW_HEIGHT, GUTTER_4PX);
        private static Grid CreateComplexSideGroup3() => BuildManifestGrid(new[] { ComplexSideGroup3Row1 }, ROW_HEIGHT, GUTTER_4PX);
        private static Grid CreateComplexSideGroup4() => BuildManifestGrid(new[] { ComplexSideGroup4Row1, ComplexSideGroup4Row2, ComplexSideGroup4Row3 }, ROW_HEIGHT, GUTTER_4PX);
        private static Grid CreateComplexSideGroup5() => BuildManifestGrid(new[] { ComplexSideGroup5Row1, ComplexSideGroup5Row2 }, ROW_HEIGHT, GUTTER_4PX);
        private static Grid CreateComplexSideGroup6() => BuildManifestGrid(new[] { ComplexSideGroup6Row1 }, ROW_HEIGHT, GUTTER_4PX);

        private static Grid CreateComplexTopGroup1() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType1, CreateComplexTopRowType2, CreateComplexTopRowType3 });
        private static Grid CreateComplexTopGroup2() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType3, CreateComplexTopRowType4, CreateComplexTopRowType4 });
        private static Grid CreateComplexTopGroup3() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType2, CreateComplexTopRowType1, CreateComplexTopRowType3 });
        private static Grid CreateComplexTopGroup4() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType2, CreateComplexTopRowType3, CreateComplexTopRowType1 });
        private static Grid CreateComplexTopGroup5() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType4, CreateComplexTopRowType3, CreateComplexTopRowType4 });
        private static Grid CreateComplexTopGroup6() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType3, CreateComplexTopRowType2, CreateComplexTopRowType1 });
        private static Grid CreateComplexTopGroup7() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType4, CreateComplexTopRowType4, CreateComplexTopRowType3 });
        private static Grid CreateComplexTopGroup8() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType1, CreateComplexTopRowType3, CreateComplexTopRowType2 });
        private static Grid CreateComplexTopGroup9() => BuildSequenceGrid(new List<Func<StackPanel>> { CreateComplexTopRowType3, CreateComplexTopRowType1, CreateComplexTopRowType2 });
        #endregion

        #region Row Factories (StackPanels)

        private static StackPanel CreateComplexTopRowType1() => CreateManifestStack(new[] { ExpStrs.x6, ExpStrs.x18, ExpStrs.x3 }, GUTTER_4PX);
        private static StackPanel CreateComplexTopRowType2() => CreateManifestStack(new[] { ExpStrs.x6, ExpStrs.x6, ExpStrs.x6, ExpStrs.x3, ExpStrs.x6 }, GUTTER_4PX);
        private static StackPanel CreateComplexTopRowType3() => CreateManifestStack(new[] { ExpStrs.x30 }, 0);
        private static StackPanel CreateComplexTopRowType4() => CreateManifestStack(new[] { ExpStrs.x18 }, 0);

        #endregion

        #region Core Rendering Engines (The "Assembly Line")

        /// <summary>
        /// Engine A: Build a Grid from a collection of string manifests (Button Rows)
        /// </summary>
        private static Grid BuildManifestGrid(string[][] rows, double rowH, double rowGutter)
        {
            var grid = new Grid { UseLayoutRounding = true };
            int rowIdx = -1;

            for (int i = 0; i < rows.Length; i++)
            {
                var rowPanel = CreateManifestStack(rows[i], rowGutter);
                AddElementToGridRow(grid, rowPanel, ref rowIdx, new GridLength(rowH));

                if (i < rows.Length - 1 && rowGutter > 0)
                    AddElementToGridRow(grid, CreateHorizontalGutter(rowGutter), ref rowIdx, new GridLength(rowGutter));
            }
            return grid;
        }

        /// <summary>
        /// Engine B: Build a Grid from a sequence of Panel-creating methods
        /// </summary>
        private static Grid BuildSequenceGrid(List<Func<StackPanel>> creators)
        {
            var grid = new Grid { UseLayoutRounding = true };
            int rowIdx = -1;
            for (int i = 0; i < creators.Count; i++)
            {
                AddCustomRowWithGutter(grid, creators[i](), ref rowIdx, ROW_HEIGHT, (i == creators.Count - 1) ? -1 : GUTTER_4PX);
            }
            return grid;
        }

        /// <summary>
        /// Engine C: Build a horizontal StackPanel from a single row manifest
        /// </summary>
        private static StackPanel CreateManifestStack(string[] layout, double gutterW)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            for (int i = 0; i < layout.Length; i++)
            {
                bool isLast = (i == layout.Length - 1);
                bool nextIsDropdown = (!isLast && layout[i + 1] == ExpStrs.x3);
                double currentGutter = (isLast || nextIsDropdown) ? -1 : gutterW;

                panel.Children.Add(ButtonFactory.CreateButton(layout[i], 0, i)); // Logical coords handled by factory
                if (currentGutter > 0) panel.Children.Add(CreateVerticalGutter(currentGutter));
            }
            return panel;
        }

        /// <summary>
        /// Engine D: Build a vertical StackPanel from a manifest (used for Side Grids)
        /// </summary>
        private static StackPanel CreateVerticalManifestStack(string[] layout, double gutterH)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            foreach (var type in layout)
            {
                panel.Children.Add(ButtonFactory.CreateButton(type, 0, 0));
                panel.Children.Add(CreateHorizontalGutter(gutterH));
            }
            return panel;
        }
        #endregion

        #region Low-Level UI Helpers

        private static void AddElementToGridRow(Grid grid, FrameworkElement element, ref int rowIdx, GridLength height)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = height });
            Grid.SetRow(element, ++rowIdx);
            grid.Children.Add(element);
        }

        private static void AddCustomRowWithGutter(Grid grid, StackPanel row, ref int rowIdx, double h, double gH)
        {
            AddElementToGridRow(grid, row, ref rowIdx, new GridLength(h));
            if (gH > 0) AddElementToGridRow(grid, CreateHorizontalGutter(gH), ref rowIdx, new GridLength(gH));
        }

        private static void AddGroupWithGutter(Grid grid, FrameworkElement group, ref int colIdx, double gW = -1)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(group, ++colIdx);
            grid.Children.Add(group);

            if (gW > 0)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gW) });
                var gutter = CreateVerticalGutter(gW);
                Grid.SetColumn(gutter, ++colIdx);
                grid.Children.Add(gutter);
            }
        }

        private static void AddRowGroupWithGutter(Grid grid, FrameworkElement group, ref int rowIdx, double gH = -1)
        {
            AddElementToGridRow(grid, group, ref rowIdx, GridLength.Auto);
            if (gH > 0) AddElementToGridRow(grid, CreateHorizontalGutter(gH), ref rowIdx, new GridLength(gH));
        }

        private static Rectangle CreateHorizontalGutter(double h) => new Rectangle { Height = h, HorizontalAlignment = HorizontalAlignment.Stretch };
        private static Rectangle CreateVerticalGutter(double w) => new Rectangle { Width = w, VerticalAlignment = VerticalAlignment.Stretch };

        #endregion
    }
}
