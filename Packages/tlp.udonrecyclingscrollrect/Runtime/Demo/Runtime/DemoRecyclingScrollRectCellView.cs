using JetBrains.Annotations;
using TLP.UdonRecyclingScrollRect.Runtime.Interfaces;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

//Cell class for demo. A cell in Recycling Scroll Rect must have a cell class inheriting from ICell.
//The class is required to configure the cell(updating UI elements etc) according to the data during recycling of cells.
//The configuration of a cell is done through the DataSource SetCellData method.
//Check RecyclingScrollerDemo class
namespace TLP.RecyclingScrollRect.Demo.Runtime
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(DemoRecyclingScrollRectCellView), ExecutionOrder)]
    public class DemoRecyclingScrollRectCellView : RecyclingScrollRectCellView
    {
        #region ExecutionOrder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = RecyclingScrollRectCellView.ExecutionOrder + 1;
        #endregion


        //UI
        public Text nameLabel;
        public Text genderLabel;
        public Text idLabel;

        public UdonEvent onClickEvent;

        //Model
        private int _cellIndex;

        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            //Can also be done in the inspector
            if (!Utilities.IsValid(onClickEvent)) {
                Error($"{nameof(onClickEvent)} not set");
                return false;
            }

            if (!onClickEvent.AddListenerVerified(this, nameof(ButtonListener))) {
                Error($"Failed to listen to {onClickEvent.GetScriptPathInScene()}");
                return false;
            }

            return true;
        }

        //This is called from the SetCell method in DataSource
        public void ConfigureCell(
                int cellIndex,
                string contactInfoName,
                string contactInfoGender,
                string contactInfoID
        ) {
            if (!HasStartedOk) {
                return;
            }

            _cellIndex = cellIndex;

            nameLabel.text = contactInfoName;
            genderLabel.text = contactInfoGender;
            idLabel.text = contactInfoID;
        }

        public override void OnEvent(string eventName) {
            switch (eventName) {
                case nameof(ButtonListener):
                    ButtonListener();
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }

        private void ButtonListener() {
            DebugLog($"Index : {_cellIndex}, Name : {nameLabel.text}, Gender : {genderLabel.text}");
        }

        public override void OnModelChanged() {
        }
    }
}