using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Multi.Cursor
{

    public interface IBlockHandler
    {
        bool FindPositionsForActiveBlock();
        bool FindPositionsForTrial(Trial trial);
        void BeginActiveBlock();
        void ShowActiveTrial();
        void EndActiveTrial(Experiment.Result result);
        void GoToNextTrial();

        void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e);
        void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e);
        void OnStartMouseEnter(Object sender, MouseEventArgs e);
        void OnStartMouseLeave(Object sender, MouseEventArgs e);
        void OnStartMouseDown(Object sender, MouseButtonEventArgs e);
        void OnStartMouseUp(Object sender, MouseButtonEventArgs e);
        void OnTargetMouseDown(Object sender, MouseButtonEventArgs e);
        void OnTargetMouseUp(Object sender, MouseButtonEventArgs e);
    }

}
