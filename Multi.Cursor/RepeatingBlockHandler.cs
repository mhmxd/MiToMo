using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Multi.Cursor
{
    public class RepeatingBlockHandler : IBlockHandler
    {
        private MainWindow _mainWindow;
        private Block _activeBlock;
        private int _activeTrialNum = 0;
        private Trial _activeTrial;
        private Dictionary<int, int> _trialTargetIds = new Dictionary<int, int>(); // Trial Id -> Target Button Id
        private Dictionary<int, Dictionary<int, Point>> _trialStartPositions = new Dictionary<int, Dictionary<int, Point>>(); // Trial Id -> [Dist (px) -> Position]

        private int _nStartClicks = 0; // Number of Start clicks in the current trial
        private bool _isTargetAvailable = false; // Whether the target is available for clicking

        public RepeatingBlockHandler(MainWindow mainWindow, Block activeBlock)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
        }

        public bool FindPositionsForActiveBlock()
        {
            foreach (Trial trial in _activeBlock.Trials)
            {
                if (!FindPositionsForTrial(trial))
                {
                    this.TrialInfo($"Failed to find positions for Trial#{trial.Id}");
                    return false; // If any trial fails, return false
                }
            }

            return true;
        }

        public bool FindPositionsForTrial(Trial trial)
        {
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
            int startHalfW = startW / 2;
            this.TrialInfo($"Finding positions for Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
                $"TargetMult = {trial.TargetMultiple}]");

            // Find a random target id for the active trial
            int targetId = FindRandomTargetIdForTrial(trial);
            if (targetId != -1)
            {
                _trialTargetIds[trial.Id] = targetId;
            }
            else
            {
                this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}");
                return false;
            }

            // Get the absolute position of the target center
            Point targetCenterAbsolute = _mainWindow.GetCenterAbsolutePosition(trial.TargetSide, targetId);

            // Find a Start position for each distance in the passes
            _trialStartPositions[trial.Id] = new Dictionary<int, Point>(); // Initialize the dict for this trial
            foreach (int dist in trial.Distances)
            {
                // Find a position for the Start
                Rect startConstraintRect = _mainWindow.GetStartConstraintRect();
                Point startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, dist, trial.TargetSide.GetOpposite());
                Point startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);

                this.TrialInfo($"Target: {targetCenterAbsolute}; Dist (px): {dist}; Start pos: {startPositionAbsolute}");
                if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
                {
                    this.TrialInfo($"No valid position found for Start for dist {dist}!");
                    return false;
                }
                else // Valid position found
                {
                    _trialStartPositions[trial.Id][dist] = startPositionAbsolute; // Add the position to the dictionary
                }
            }

            return true;
        }

        public void BeginActiveBlock()
        {
            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            ShowActiveTrial();
        }

        public void ShowActiveTrial()
        {
            this.TrialInfo($"Showing rep Trial#{_activeTrial.Id} | Target side: {_activeTrial.TargetSide} | First dist: {_activeTrial.Distances.First()}");

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.TargetSide);

            // Color the target button and set the handlers
            _mainWindow.FillButtonInTargetWindow(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_UNAVAILABLE_COLOR);
            _mainWindow.SetGridButtonHandlers(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], OnTargetMouseDown, OnTargetMouseUp);

            // Show the first Start
            Point firstStartPos = _trialStartPositions[_activeTrial.Id].First().Value;
            _mainWindow.ShowStart(
                firstStartPos, Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
        }

        public void EndActiveTrial()
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed with {_nStartClicks + 1} Start clicks.");

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                // Clear the current Target
                _mainWindow.ResetTargetWindow(_activeTrial.TargetSide);

                _nStartClicks = 0;
                _activeTrialNum++;

                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                ShowActiveTrial();
            }
            else
            {
                // End of block, handle accordingly (e.g., show results or reset)
                this.TrialInfo("End of block reached.");
            }
        }

        private int FindRandomTargetIdForTrial(Trial trial)
        {
            // Based on the width multiple, find a random target button id that haven't been used before
            int targetMultiple = trial.TargetMultiple;
            int targetId = -1;
            do
            {
                targetId = _mainWindow.GetRadomTargetId(trial.TargetSide, targetMultiple);
            } while (_trialTargetIds.ContainsValue(targetId));

            return targetId;

        }

        public void OnStartMouseEnter(Object sender, MouseEventArgs e)
        {
            
        }

        public void OnStartMouseLeave(Object sender, MouseEventArgs e)
        {
            
        }

        public void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
           
        }

        public void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
           
        }

        public void OnStartMouseDown(Object sender, MouseButtonEventArgs e)
        {
           
        }

        public void OnStartMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (_nStartClicks < _activeTrial.Distances.Count - 1) // Still passes left
            {
                _mainWindow.FillStart(Config.START_UNAVAILABLE_COLOR);
                _mainWindow.FillButtonInTargetWindow(
                    _activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_AVAILABLE_COLOR);
                _isTargetAvailable = true; // Target is now available for clicking
            }
            else // All Start clicks done => Trial completed
            {
                EndActiveTrial();
            }
        }

        public void OnTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (!_isTargetAvailable)
            {
                // Trial miss
            }
            else
            {
                // Nothing for now
            }
        }

        public void OnTargetMouseUp(Object sender, MouseButtonEventArgs e)
        {
            // Show the next Start
            _nStartClicks++;
            Point startAbsolutePosition = _trialStartPositions[_activeTrial.Id].Values.ToList()[_nStartClicks];
            _mainWindow.ShowStart(
                startAbsolutePosition, Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
            _mainWindow.FillButtonInTargetWindow(
                _activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_UNAVAILABLE_COLOR);
        }

    }
}
