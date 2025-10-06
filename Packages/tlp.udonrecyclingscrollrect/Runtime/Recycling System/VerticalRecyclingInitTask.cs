using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Experimental.Tasks;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonRecyclingScrollRect.Runtime.Recycling_System
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(VerticalRecyclingInitTask), ExecutionOrder)]
    public class VerticalRecyclingInitTask : Task
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = VerticalRecyclingScrollView.ExecutionOrder + 1;
        #endregion

        internal VerticalRecyclingScrollView VerticalRecyclingScrollView;

        #region State
        private int _step;
        private bool _creatingCells;
        private float _currentPoolCoverage;
        private float _posY;
        private int _poolSize;
        private int _totalItemCount;
        private float _requiredCoverage;
        private int _minPoolSize;
        #endregion

        #region Task
        protected override bool InitTask() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(InitTask));
#endif
            #endregion

            if (!Utilities.IsValid(VerticalRecyclingScrollView)) {
                Error($"{nameof(VerticalRecyclingScrollView)} not set");
                return false;
            }

            _step = 0;
            _creatingCells = false;
            _currentPoolCoverage = 0;
            _posY = 0;
            _poolSize = 0;
            _totalItemCount = 0;
            _requiredCoverage = 0;
            _minPoolSize = 0;
            return true;
        }

        protected override TaskResult DoTask(float stepDeltaTime) {
            if (_creatingCells) {
                CreateNeededCells();
                return TaskResult.Unknown;
            }

            switch (_step) {
                case 0:
                    VerticalRecyclingScrollView.ResetCachedCellViews();
                    break;
                case 1:
                    VerticalRecyclingScrollView.ResetCellViews();
                    break;
                case 2:
                    VerticalRecyclingScrollView.InitializePrototypeCell();
                    VerticalRecyclingScrollView.InitializeState();

                    //Get the required pool coverage and minimum size for the Cell pool
                    _requiredCoverage = VerticalRecyclingScrollView.GetRequiredCoverage();
                    _minPoolSize = VerticalRecyclingScrollView.GetMinPoolSize();

                    //create cells until the Pool area is covered and pool size is the minimum required
                    _currentPoolCoverage = 0;
                    _posY = 0;
                    _totalItemCount = VerticalRecyclingScrollView.DataSource.GetItemCount();
                    _poolSize = 0;
                    _creatingCells = true;
                    break;
                default:
                    VerticalRecyclingScrollView.FinalizeInitialization();
                    return TaskResult.Succeeded;
            }

            ++_step;
            return TaskResult.Unknown;
        }
        #endregion

        #region Internal
        internal void CreateNeededCells() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(CreateNeededCells));
#endif
            #endregion

            if (!Utilities.IsValid(VerticalRecyclingScrollView.DataSource)) {
                Error($"{nameof(VerticalRecyclingScrollView.DataSource)} invalid");
                return;
            }

            if (!VerticalRecyclingScrollView.PoolIsNotFilled(
                        _poolSize,
                        _minPoolSize,
                        _currentPoolCoverage,
                        _requiredCoverage,
                        _totalItemCount
                )) {
                _creatingCells = false;
                VerticalRecyclingScrollView.DeactivateCellPrototype();
                return;
            }

            var createdRectTransform = VerticalRecyclingScrollView.AddNewCellToPool();
            VerticalRecyclingScrollView.PositionCell(createdRectTransform, ref _posY, ref _currentPoolCoverage);
            VerticalRecyclingScrollView.AddCellWithData(createdRectTransform, _poolSize);
            ++_poolSize;

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(CreateNeededCells)}: {nameof(_poolSize)}={_poolSize}");
#endif
            #endregion


            VerticalRecyclingScrollView.UpdateBottomMostCellColumn();
        }
        #endregion
    }
}