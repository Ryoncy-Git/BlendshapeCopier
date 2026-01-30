using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

public class BlendShapeCopier : EditorWindow
{
    private AnimationClip sourceClip;
    private AnimationClip targetClip;
    // private AnimationClip[] targetClips;
    private List<AnimationClip> targetClips = new List<AnimationClip>();
    private DefaultAsset targetFolder;
    private OverwriteMode overwriteMode = OverwriteMode.KeepTargetIfExists;
    private bool isFolder;
    private bool isMulti;
    private ApplyRange applyRange = ApplyRange.Single;
    private enum ApplyRange
    {
        Single = 1, // 単数に適応
        Folder = 2, // フォルダ内のアニメーションに適応
        Multiple = 3, // 複数を選択して適応
    };

    private enum OverwriteMode
    {
        KeepTargetIfExists,  // 上書きしない
        OverwriteIfExists,   // 上書き
        PreferLargerValue,   // 大きいほうを優先
        PreferSmallerValue   // 小さいほうを優先
    }

    [MenuItem("Ryoncy/BlendShape Copier Menu")]
    public static void ShowWindow()
    {
        GetWindow<BlendShapeCopier>("BlendShape Copier Menu");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("変更内容の入ったアニメーションファイル", EditorStyles.boldLabel);
        GUILayout.Label("適応するシェイプキーが入ったアニメーションを入れてください");
        sourceClip = (AnimationClip)EditorGUILayout.ObjectField("コピー元", sourceClip, typeof(AnimationClip), false);
        GUILayout.Space(10);

        // isFolder = (bool)EditorGUILayout.Toggle("複数のアニメーションに適応する場合", isFolder);
        GUILayout.Label("変更を加える範囲の選択", EditorStyles.boldLabel);
        GUILayout.Label("ひとつのファイルにのみ適応する場合 ： Single");
        GUILayout.Label("複数のファイルに適応する場合（フォルダ内すべてに適応） ： Folder");
        GUILayout.Label("複数のファイルに適応する場合（選択したファイルすべてに適応） ： Multiple");
        applyRange = (ApplyRange)EditorGUILayout.EnumPopup("適応範囲", applyRange);

        GUILayout.Space(10);


        // if (isFolder)
        // {
        //     GUILayout.Label("変更先のアニメーションが入ったフォルダ", EditorStyles.boldLabel);
        //     GUILayout.Label("フォルダ内のアニメーションすべてに適応します");
        //     targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("コピー先", targetFolder, typeof(DefaultAsset), false);
        // }
        // else
        // {
        //     GUILayout.Label("変更先のアニメーションファイル", EditorStyles.boldLabel);
        //     GUILayout.Label("このアニメーションファイルの内容を変更します");
        //     targetClip = (AnimationClip)EditorGUILayout.ObjectField("コピー先", targetClip, typeof(AnimationClip), false);
        // }

        switch(applyRange)
        {
            case ApplyRange.Single:
                GUILayout.Label("変更先のアニメーションファイル", EditorStyles.boldLabel);
                GUILayout.Label("このアニメーションファイルの内容を変更します");
                targetClip = (AnimationClip)EditorGUILayout.ObjectField("コピー先", targetClip, typeof(AnimationClip), false);
                break;

            case ApplyRange.Folder:
                GUILayout.Label("変更先のアニメーションが入ったフォルダ", EditorStyles.boldLabel);
                GUILayout.Label("フォルダ内のアニメーションすべてに適応します");
                targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("コピー先", targetFolder, typeof(DefaultAsset), false);
            break;

            case ApplyRange.Multiple:
                GUILayout.Label("変更先のアニメーションファイル", EditorStyles.boldLabel);
                GUILayout.Label("登録したすべてのアニメーションファイルの内容を変更します");
                GUILayout.Label("＋ボタンを押して要素数を追加、－ボタンで要素数を減らせます (最大10個まで)");

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    if(targetClips.Count > 0)
                        targetClips.RemoveAt(targetClips.Count - 1);
                }
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    if(targetClips.Count < 10)
                        targetClips.Add(null);
                }
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < targetClips.Count; i++)
                {
                    targetClips[i] = (AnimationClip)EditorGUILayout.ObjectField(
                        targetClips[i],
                        typeof(AnimationClip),
                        false
                    );
                }
            break;
        }



        GUILayout.Space(30);
        GUILayout.Label("上書きモードの選択", EditorStyles.boldLabel);
        GUILayout.Label("変更先に同じBlendShapeがある場合");
        GUILayout.Label("コピー先を優先する場合 : Keep Target If Exist");
        GUILayout.Label("変更内容を優先する場合 : Overwrite If Exist");
        GUILayout.Label("値の大きい方を優先する場合 : Prefer Larger Value");
        GUILayout.Label("値の小さい方を優先する場合 : Prefer Smaller Value");
        overwriteMode = (OverwriteMode)EditorGUILayout.EnumPopup("上書きモード", overwriteMode);

        GUILayout.Space(50);
        GUILayout.Label("アニメーションファイルを書き換える形で変更します", EditorStyles.boldLabel);
        GUILayout.Label("表情のアニメーションファイルはバックアップを取っておくことを推奨します", EditorStyles.boldLabel);
        GUILayout.Space(10);

        switch(applyRange)
        {
            case ApplyRange.Single:
                if(GUILayout.Button("適応させる"))
                {
                    if (sourceClip == null || targetClip == null)
                    {
                        EditorUtility.DisplayDialog
                        (
                            "オブジェクトが指定されていません",
                            "コピー元アニメーションとコピー先アニメーションを指定してください",
                            "OK"
                        );
                        return;
                    }


                    if (targetClip == sourceClip)
                        return;

                    CopyBlendShapes(sourceClip, targetClip, overwriteMode);

                    EditorUtility.DisplayDialog
                    (
                        "コピー完了",
                        "AnimationClip に BlendShape をコピーしました",
                        "OK"
                    );
                }
                break;

            case ApplyRange.Folder:
                if(GUILayout.Button("フォルダ内のすべてのアニメーションファイルに適応させる"))
                {
                    if (sourceClip == null || targetFolder == null)
                    {
                        EditorUtility.DisplayDialog
                        (
                            "オブジェクトが指定されていません",
                            "コピー元アニメーションとコピー先フォルダを指定してください",
                            "OK"
                        );
                        return;
                    }


                    string folderPath = AssetDatabase.GetAssetPath(targetFolder);
                    string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });

                    int count = 0;
                    foreach (string guid in animGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                        if (targetClip == sourceClip || targetClip == null)
                            continue;

                        CopyBlendShapes(sourceClip, targetClip, overwriteMode);
                        count++;
                    }

                    EditorUtility.DisplayDialog
                    (
                        "コピー完了",
                        $"{count}個の AnimationClip に BlendShape をコピーしました",
                        "OK"
                    );
                }
                break;

            case ApplyRange.Multiple:
                if(GUILayout.Button("選択したすべてのアニメーションファイルに適応させる"))
                {
                    if (sourceClip == null || targetClips.Count == 0)
                    {
                        EditorUtility.DisplayDialog
                        (
                            "オブジェクトが指定されていません",
                            "コピー元アニメーションとコピー先アニメーションを指定してください",
                            "OK"
                        );
                        return;
                    }

                    foreach (AnimationClip target in targetClips)
                    {
                        if(target == sourceClip || target == null)
                            continue;
                        
                        CopyBlendShapes(sourceClip, target, overwriteMode);
                    }


                    EditorUtility.DisplayDialog
                    (
                        "コピー完了",
                        "AnimationClip に BlendShape をコピーしました",
                        "OK"
                    );
                }
                break;
        }
    }

    private void CopyBlendShapes(AnimationClip source, AnimationClip target, OverwriteMode mode)
    {
        Undo.RecordObject(target, "Copy BlendShapes");

        var targetBindings = new Dictionary<string, AnimationCurve>();
        foreach (var binding in AnimationUtility.GetCurveBindings(target))
        {
            if (binding.type == typeof(SkinnedMeshRenderer) && binding.propertyName.StartsWith("blendShape."))
            {
                var curve = AnimationUtility.GetEditorCurve(target, binding);
                targetBindings[binding.propertyName] = curve;
            }
        }

        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            if (binding.type != typeof(SkinnedMeshRenderer) || !binding.propertyName.StartsWith("blendShape."))
                continue;

            AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
            if (sourceCurve == null || IsAllZero(sourceCurve))
                continue; // 値がすべて0のカーブは無視

            bool alreadyExists = targetBindings.TryGetValue(binding.propertyName, out AnimationCurve targetCurve);

            if (alreadyExists)
            {
                if (mode == OverwriteMode.KeepTargetIfExists)
                    continue;

                if (mode == OverwriteMode.PreferLargerValue && IsCurveAverageGreater(targetCurve, sourceCurve))
                    continue;

                if (mode == OverwriteMode.PreferSmallerValue && !IsCurveAverageGreater(targetCurve, sourceCurve))
                    continue;
            }

            AnimationUtility.SetEditorCurve(target, binding, sourceCurve);
        }

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
    }

    private bool IsAllZero(AnimationCurve curve)
    {
        foreach (var key in curve.keys)
        {
            if (Mathf.Abs(key.value) > 0.0001f)
                return false;
        }
        return true;
    }

    private bool IsCurveAverageGreater(AnimationCurve a, AnimationCurve b)
    {
        float avgA = GetAverageValue(a);
        float avgB = GetAverageValue(b);
        return avgA > avgB;
    }

    private float GetAverageValue(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0) return 0f;

        float sum = 0f;
        foreach (var key in curve.keys)
            sum += key.value;

        return sum / curve.length;
    }

}