using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using R3;

public class PuzzleFinderView : MonoBehaviour
{
    private readonly ReactiveProperty<PuzzleState?> _appliedState = new(null);
    public ReadOnlyReactiveProperty<PuzzleState?> AppliedState => _appliedState;

    [SerializeField] private GameObject inputFieldParent;
    [SerializeField] private GameObject dropdownParent;
    [SerializeField] private TMP_Dropdown[] blockNumberDropdown;
    [SerializeField] private TMP_InputField[] blockNumberInputField;
    [SerializeField] private Toggle dropdownToggle;

    [SerializeField] private bool dropdownSelectionMode = false;
    
    BlockNumber[,] dropdownBlockNumbers = new BlockNumber[PuzzleState.RowCount, PuzzleState.ColumnCount];
    BlockNumber[,] inputFieldBlockNumbers = new BlockNumber[PuzzleState.RowCount, PuzzleState.ColumnCount];
    private List<TMP_Dropdown.OptionData>[] firstOptionData = new List<TMP_Dropdown.OptionData>[PuzzleState.TotalCells];

    private void Awake(){
        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int col = 0; col < PuzzleState.ColumnCount; col++)
            {
                BlockPosition blockPosition = new BlockPosition(row, col);
                
                firstOptionData[blockPosition] = new List<TMP_Dropdown.OptionData>(blockNumberDropdown[blockPosition].options);
                blockNumberDropdown[blockPosition].onValueChanged.AddListener(number => OnDropdownValueChanged(number, blockPosition));
                blockNumberInputField[blockPosition].onValueChanged.AddListener(chara => OnInputFieldValueChanged(chara, blockPosition));
                dropdownBlockNumbers[row, col] = BlockNumber.Zero();
                inputFieldBlockNumbers[row, col] = BlockNumber.Zero();
            }
        }

        dropdownToggle.onValueChanged.AddListener(SwitchMode);
        _appliedState.AddTo(this);//いる？
    }

    public void SetPuzzle(PuzzleState puzzleState)
    {
        for (int row = 0; row < PuzzleState.RowCount; row++)
        {
            for (int col = 0; col < PuzzleState.ColumnCount; col++)
            {
                BlockPosition blockPosition = new BlockPosition(row, col);
                
                dropdownBlockNumbers[row, col] = puzzleState[blockPosition];
                inputFieldBlockNumbers[row, col] = puzzleState[blockPosition];
                blockNumberDropdown[blockPosition].ClearOptions();
                blockNumberDropdown[blockPosition].AddOptions(firstOptionData[blockPosition]);
                blockNumberDropdown[blockPosition].value = puzzleState[blockPosition];
                blockNumberInputField[blockPosition].text = puzzleState[blockPosition].ToString();
            }
        }
    }

    private void SwitchMode(bool isOn)
    {
        inputFieldParent.SetActive(!isOn);
        dropdownParent.SetActive(isOn);
    }

    private void OnDropdownValueChanged(int number, int index){
        string newItemText = blockNumberDropdown[index].options[number].text;
        BlockPosition blockPosition = BlockPosition.CreateFromIndex(index);
        if (dropdownSelectionMode)
        {
            BlockNumber blockNumber = dropdownBlockNumbers[blockPosition.Row, blockPosition.Column];
            string lastItemText = blockNumber.ToString();

            for (int i = 0; i < blockNumberDropdown.Length; i++)
            {
                if (i == index)
                {
                    continue;
                }

                if (blockNumber.ToString() != "")
                {
                    int insertIndex = blockNumberDropdown[i].options
                        .FindIndex(x => BlockNumber.Parse(x.text) > blockNumber);
                    TMP_Dropdown.OptionData newOptionData = new TMP_Dropdown.OptionData(lastItemText);
                    insertIndex = insertIndex != -1 ? insertIndex : blockNumberDropdown[i].options.Count - 1;
                    blockNumberDropdown[i].options.Insert(insertIndex, newOptionData);
                }

                if (newItemText != "")
                {
                    var optionData = blockNumberDropdown[i].options.Find(x => x.text == newItemText);
                    blockNumberDropdown[i].options.Remove(optionData);
                }
            }
        }
        int newBlockNumber = BlockNumber.Parse(newItemText);
        dropdownBlockNumbers[blockPosition.Row, blockPosition.Column] = new BlockNumber(newBlockNumber);
    }
    
    private void OnInputFieldValueChanged(string chara, int index){
        int newBlockNumber = BlockNumber.Parse(chara);
        BlockPosition blockPosition = BlockPosition.CreateFromIndex(index);
        inputFieldBlockNumbers[blockPosition.Row, blockPosition.Column] = new BlockNumber(newBlockNumber);
    }

    /// <summary>
    /// Applyボタンから呼ばれるメソッド
    /// 現在の状態をリアクティブストリームで発火
    /// </summary>
    public void OnApplyButtonClicked()
    {
        if (!PuzzleState.TryCreate(dropdownToggle.isOn ? dropdownBlockNumbers : inputFieldBlockNumbers, out var currentState))
            return;

        _appliedState.OnNext(currentState);
        Debug.Log($"Apply!{_appliedState.Value}");
    }
}
