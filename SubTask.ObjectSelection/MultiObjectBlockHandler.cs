using HarfBuzzSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static SubTask.ObjectSelection.Experiment;
using static SubTask.ObjectSelection.Utils;

namespace SubTask.ObjectSelection
{
    public class MultiObjectBlockHandler
    {
        private bool _isTargetAvailable = false; // Whether the target is available for clicking
        private int _pressedObjectId = -1; // Id of the object that was pressed in the current trial
        private int _markedObjectId = -1; // Id of the object that was marked in the current trial
        private bool _isFunctionClicked = false; // Whether the function button was clicked (for mouse)

        private const string CacheDirectory = "TrialPositionCache";
        private const int MaxCachedPositions = 100;

        public MultiObjectBlockHandler(MainWindow mainWindow, Block activeBlock)
        {
            //_mainWindow = mainWindow;
            //_activeBlock = activeBlock;

            //// Create records for all trials in the block
            //foreach (Trial trial in _activeBlock.Trials)
            //{
            //    _trialRecords[trial.Id] = new TrialRecord();
            //}

            //// Make sure the required directory exists
            //if (!Directory.Exists(CacheDirectory))
            //{
            //    Directory.CreateDirectory(CacheDirectory);
            //    this.TrialInfo($"Created cache directory at: {Path.GetFullPath(CacheDirectory)}");
            //}
        }


    }
}
