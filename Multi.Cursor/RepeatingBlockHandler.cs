using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private int _startClickedCount = 0;
        private bool _isTargetAvailable = false; // Whether the target is available for clicking

        private Stopwatch _trialtWatch = new Stopwatch();
        private Dictionary<string, long> _trialTimestamps = new Dictionary<string, long>(); // Trial timestamps for logging

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
            //int targetId = FindRandomTargetIdForTrial(trial);
            int targetId = _mainWindow.GetRadomTargetId(trial.TargetSide, trial.TargetMultiple);
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

            // Get Start constraints
            Rect startConstraintRect = _mainWindow.GetStartConstraintRect();

            // Find a Start position for each distance in the passes
            _trialStartPositions[trial.Id] = new Dictionary<int, Point>();
            Point firstStartCenter = new Point(-1, -1); // Other positions must be close the first one
            int maxRetries = 100; // Max number of retries to find a valid Start position
            int nRetries = 0;
            foreach (int dist in trial.Distances)
            {
                Point startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, dist, trial.TargetSide.GetOpposite());

                if (firstStartCenter.X != -1) // Not the first Start
                {
                    
                    while (Utils.Dist(startCenter, firstStartCenter) > Utils.MM2PX(Experiment.REP_TRIAL_MAX_DIST_STARTS_MM))
                    {
                        this.TrialInfo($"Distance to first Start = {Utils.PX2MM(Utils.Dist(startCenter, firstStartCenter))}");
                        if (nRetries >= maxRetries)
                        {
                            this.TrialInfo($"Failed to find a valid Start position for dist {dist} after {maxRetries} retries!");
                            return false; // Failed to find a valid position
                        }
                        startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, dist, trial.TargetSide.GetOpposite());
                        nRetries++;
                    }
                } 
                else
                {
                    firstStartCenter = startCenter; // Save the first Start position
                }

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
            this.TrialInfo("------------------- Beginning block ----------------------------");
            // List all the trials in the block
            foreach (Trial trial in _activeBlock.Trials)
            {
                this.TrialInfo($"Trial#{trial.Id} | Target side: {trial.TargetSide} | Distances: {trial.Distances}");
            }
            _trialtWatch.Restart();
            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            ShowActiveTrial();
        }

        public void ShowActiveTrial()
        {
            this.TrialInfo(Str.MINOR_LINE);
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

        public void EndActiveTrial(Experiment.Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed.");
            this.TrialInfo(Str.MAJOR_LINE);
            _trialTimestamps[Str.TRIAL_END] = _trialtWatch.ElapsedMilliseconds; // Log the trial end timestamp

            switch (result)
            {
                case Experiment.Result.HIT:
                    Sounder.PlayHit();
                    GoToNextTrial();
                    break;
                case Experiment.Result.MISS:
                    Sounder.PlayTargetMiss();
                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    GoToNextTrial();
                    break;
                case Experiment.Result.NO_START:
                    Sounder.PlayStartMiss();
                    // Do nothing, just reset everything

                    break;
            }

            

        }

        public void GoToNextTrial()
        {
            _mainWindow.ResetTargetWindow(_activeTrial.TargetSide); // Reset the target window
            _startClickedCount = 0; // Reset the Start clicks for the next trial
            _isTargetAvailable = false; // Reset the target availability

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                ShowActiveTrial();
            }
            else
            {
                this.TrialInfo("All trials in the block completed.");
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
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {

            }
            else //-- Mouse
            {

            }
        }

        public void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
           
        }

        public void OnStartMouseDown(Object sender, MouseButtonEventArgs e)
        {
           
        }

        public void OnStartMouseUp(Object sender, MouseButtonEventArgs e)
        {
            _startClickedCount++;

            if (_startClickedCount == _activeTrial.Distances.Count) // All passes done
            {
                EndActiveTrial(Experiment.Result.HIT);
                
            }
            else // Still passes left
            {
                _mainWindow.FillStart(Config.START_UNAVAILABLE_COLOR);
                _mainWindow.FillButtonInTargetWindow(
                    _activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_AVAILABLE_COLOR);
                _isTargetAvailable = true; // Target is now available for clicking
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
            Point startAbsolutePosition = _trialStartPositions[_activeTrial.Id].Values.ToList()[_startClickedCount];
            _mainWindow.ShowStart(
                startAbsolutePosition, Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
            _mainWindow.FillButtonInTargetWindow(
                _activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_UNAVAILABLE_COLOR);
        }

    }
}
