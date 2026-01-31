using UnityEditor;
using UnityEngine;

/// <summary>
/// SerializablePuzzleStateのCustom PropertyDrawer
/// Inspector上でNxNグリッドとして綺麗に表示する
/// </summary>
[CustomPropertyDrawer(typeof(SerializablePuzzleState))]
public class SerializablePuzzleStateDrawer : PropertyDrawer
{
    private const float CellSize = 40f;
    private const float CellSpacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // ラベルを表示
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // _isEnabledプロパティを取得
        SerializedProperty isEnabledProperty = property.FindPropertyRelative("_isEnabled");

        // チェックボックスの描画（グリッドの左上に表示）
        Rect checkboxRect = new Rect(position.x, position.y, 20f, EditorGUIUtility.singleLineHeight);
        isEnabledProperty.boolValue = EditorGUI.Toggle(checkboxRect, isEnabledProperty.boolValue);

        // "Enabled" ラベルを追加
        Rect labelRect = new Rect(position.x + 25f, position.y, 60f, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, "Enabled");

        // グリッドの開始位置を調整（チェックボックスの下）
        position.y += EditorGUIUtility.singleLineHeight + CellSpacing;

        // _blocksプロパティを取得
        SerializedProperty blocksProperty = property.FindPropertyRelative("_blocks");

        // 配列が初期化されていない場合は初期化
        if (blocksProperty.arraySize != PuzzleState.TotalCells)
        {
            blocksProperty.arraySize = PuzzleState.TotalCells;
            // デフォルトのゴール状態で初期化
            int[] defaultState = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
            for (int i = 0; i < PuzzleState.TotalCells; i++)
            {
                blocksProperty.GetArrayElementAtIndex(i).intValue = defaultState[i];
            }
        }

        // チェックが入っている場合のみグリッドを描画
        if (isEnabledProperty.boolValue)
        {
            // インデントをリセット（PrefixLabelの後なので）
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // NxNグリッドを描画
            for (int row = 0; row < PuzzleState.RowCount; row++)
            {
                for (int col = 0; col < PuzzleState.ColumnCount; col++)
                {
                    Rect cellRect = new Rect(
                        position.x + col * (CellSize + CellSpacing),
                        position.y + row * (CellSize / 2 + CellSpacing),
                        CellSize,
                        CellSize / 2
                    );

                    int index = row * PuzzleState.ColumnCount + col;
                    SerializedProperty element = blocksProperty.GetArrayElementAtIndex(index);

                    int value = EditorGUI.IntField(cellRect, element.intValue);

                    // 値を0-8の範囲に制限
                    value = Mathf.Clamp(value, 0, PuzzleState.TotalCells - 1);
                    element.intValue = value;
                }
            }

            // インデントを元に戻す
            EditorGUI.indentLevel = indent;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty isEnabledProperty = property.FindPropertyRelative("_isEnabled");

        // チェックボックスの高さ
        float height = EditorGUIUtility.singleLineHeight;

        // チェックが入っている場合のみグリッドの高さを追加
        if (isEnabledProperty.boolValue)
        {
            height += CellSpacing + PuzzleState.RowCount * (CellSize / 2 + CellSpacing) - CellSpacing;
        }

        return height;
    }
}
