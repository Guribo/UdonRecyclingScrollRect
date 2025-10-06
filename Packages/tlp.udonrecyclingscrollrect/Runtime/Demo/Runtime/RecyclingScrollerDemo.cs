using JetBrains.Annotations;
using TLP.UdonRecyclingScrollRect.Runtime.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
using Random = UnityEngine.Random;

namespace TLP.RecyclingScrollRect.Demo.Runtime
{
    /// <summary>
    /// Demo controller class for Recycling Scroll Rect. 
    /// A controller class is responsible for providing the scroll rect with datasource. Any class can be a controller class. 
    /// The only requirement is to inherit from IRecyclingScrollRectDataSource and implement the interface methods
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(RecyclingScrollerDemo), ExecutionOrder)]
    public class RecyclingScrollerDemo : RecyclingScrollRectDataSource
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = RecyclingScrollRectDataSource.ExecutionOrder + 1;
        #endregion

        [SerializeField]
        UdonRecyclingScrollRect.Runtime.RecyclingScrollRect RecyclingScrollRect;

        [FormerlySerializedAs("dataLength")]
        [SerializeField]
        private int DataLength;

        //Dummy data List
        private string[] _names;
        private string[] _genders;
        private string[] _ids;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (!Utilities.IsValid(RecyclingScrollRect)) {
                Error($"{nameof(RecyclingScrollRect)} not set");
                return false;
            }

            InitData();
            RecyclingScrollRect.Initialize(this);
            return true;
        }

        //Initialising _contactList with dummy data 
        private void InitData() {
            _names = new string[DataLength];
            _genders = new string[DataLength];
            _ids = new string[DataLength];

            var genderOptions = new string[] { "Male", "Female" };
            for (var i = 0; i < DataLength; ++i) {
                _names[i] = $"{i}_Name";
                _genders[i] = genderOptions[Random.Range(0, 2)];
                _ids[i] = $"item : {i}";
            }
        }

        #region DATA-SOURCE
        /// <summary>
        /// Data source method. return the list length.
        /// </summary>
        public override int GetItemCount() {
            if (_names == null) {
                return 0;
            }

            return _names.Length;
        }

        /// <summary>
        /// Data source method. Called for a cell every time it is recycled.
        /// Implement this method to do the necessary cell configuration.
        /// </summary>
        public override void SetCell(RecyclingScrollRectCellView recyclingScrollRectCellView, int index) {
            //Casting to the implemented Cell
            var item = (DemoRecyclingScrollRectCellView)recyclingScrollRectCellView;
            if (!item) {
                Error($"cell is not of type {GetUdonTypeName<DemoRecyclingScrollRectCellView>()}");
                return;
            }

            item.ConfigureCell(index, _names[index], _genders[index], _ids[index]);
        }
        #endregion
    }
}