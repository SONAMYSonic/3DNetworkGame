using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 몬스터용 애니메이션 설정 에디터 도구.
///
/// Unity 메뉴에서 순서대로 실행:
///   Tools > Monster > 1. Extract & Clean Clips (클립 추출 + 이동 커브 제거)
///   Tools > Monster > 2. Create Animator Controller (컨트롤러 자동 생성)
/// </summary>
public class MonsterAnimatorCreator
{
    // ─────────────────────────────────────────────
    // FBX 경로
    // ─────────────────────────────────────────────
    private const string IDLE_FBX   = "Assets/10.Imports/IndieCat Animals/Cats/Animations/A_Cat_Idle.fbx";
    private const string MOVE_FBX   = "Assets/10.Imports/IndieCat Animals/Cats/Animations/A_Cat_Move.fbx";
    private const string COMBAT_FBX = "Assets/10.Imports/IndieCat Animals/Cats/Animations/A_Cat_Combat.fbx";

    // 추출된 클립 저장 폴더
    private const string CLIP_FOLDER = "Assets/02.Scripts/Monster/Animations";

    // 컨트롤러 출력 경로
    private const string CONTROLLER_PATH = "Assets/02.Scripts/Monster/MonsterAnimator.controller";

    // 추출할 클립 목록: (FBX경로, 원본클립이름, 저장파일명)
    private static readonly (string fbx, string clip, string file)[] CLIPS =
    {
        (IDLE_FBX,   "Base",      "Monster_Idle.anim"),
        (MOVE_FBX,   "Walk_F",    "Monster_Walk.anim"),
        (MOVE_FBX,   "Run_F",     "Monster_Run.anim"),
        (COMBAT_FBX, "Bite_R",    "Monster_Attack.anim"),
        (COMBAT_FBX, "Paw_L",     "Monster_Attack2.anim"),
        (COMBAT_FBX, "Damage.FL", "Monster_Hit.anim"),
        (COMBAT_FBX, "Death_R",   "Monster_Death.anim"),
    };

    // ═════════════════════════════════════════════
    // STEP 1: 클립 추출 + Position 커브 제거
    // ═════════════════════════════════════════════

    [MenuItem("Tools/Monster/1. Extract and Clean Clips")]
    public static void ExtractAndCleanClips()
    {
        // 폴더 생성
        if (!AssetDatabase.IsValidFolder(CLIP_FOLDER))
        {
            // 부모 폴더가 있는지 확인
            string parent = Path.GetDirectoryName(CLIP_FOLDER).Replace("\\", "/");
            string folderName = Path.GetFileName(CLIP_FOLDER);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        int successCount = 0;

        foreach (var (fbx, clipName, fileName) in CLIPS)
        {
            // 1) FBX에서 원본 클립 찾기
            AnimationClip sourceClip = LoadClipFromFBX(fbx, clipName);
            if (sourceClip == null)
            {
                Debug.LogError($"[ClipExtract] '{fbx}'에서 '{clipName}' 클립을 찾지 못했습니다.");
                continue;
            }

            // 2) 새 클립 생성 + Position 커브 제거
            AnimationClip cleanClip = CleanClip(sourceClip, clipName);

            // 3) 저장
            string savePath = $"{CLIP_FOLDER}/{fileName}";
            AssetDatabase.CreateAsset(cleanClip, savePath);
            successCount++;

            Debug.Log($"  <color=cyan>✓ {clipName} → {savePath}</color>");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>[ClipExtract] 완료! {successCount}/{CLIPS.Length}개 클립 추출됨 → {CLIP_FOLDER}/</color>");
    }

    /// <summary>
    /// 원본 클립에서 Position(이동) 커브를 제거한 깨끗한 클립을 만든다.
    ///
    /// 왜 필요한가:
    ///   고양이 Walk_F 애니메이션은 본(Pelvis 등)에 앞으로 이동하는 키프레임이 있어서
    ///   applyRootMotion=false, OnAnimatorMove()로도 막을 수 없는 경우가 있다.
    ///   커브 자체를 제거하면 애니메이션이 제자리에서 재생된다.
    /// </summary>
    private static AnimationClip CleanClip(AnimationClip source, string clipName)
    {
        AnimationClip newClip = new AnimationClip();
        newClip.name = source.name;

        // 루프 설정 복사
        AnimationClipSettings srcSettings = AnimationUtility.GetAnimationClipSettings(source);
        AnimationClipSettings newSettings = AnimationUtility.GetAnimationClipSettings(newClip);
        newSettings.loopTime = srcSettings.loopTime;
        newSettings.loopBlend = srcSettings.loopBlend;

        // Death, Hit, Attack 은 루프 하지 않음
        string lower = clipName.ToLower();
        if (lower.Contains("death") || lower.Contains("damage") ||
            lower.Contains("bite") || lower.Contains("paw"))
        {
            newSettings.loopTime = false;
        }
        else
        {
            newSettings.loopTime = true;
        }

        // ── 커브 복사 (Position 커브만 제거) ──
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
        int removedCount = 0;

        foreach (EditorCurveBinding binding in bindings)
        {
            bool shouldRemove = false;

            // Position 커브인지 확인
            // localPosition.x, .y, .z 또는 m_LocalPosition.x, .y, .z
            if (binding.propertyName.Contains("LocalPosition") ||
                binding.propertyName.Contains("localPosition") ||
                binding.propertyName.Contains("m_LocalPosition"))
            {
                // 루트 본 (path가 비어있거나 1단계 깊이) 의 Position만 제거
                // 깊은 본(다리, 꼬리 등)의 Position은 유지
                int depth = string.IsNullOrEmpty(binding.path)
                    ? 0
                    : binding.path.Count(c => c == '/') + 1;

                if (depth <= 1)
                {
                    // 이 커브의 값이 실제로 변하는지 확인 (상수면 제거할 필요 없음)
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                    if (curve != null && HasSignificantMovement(curve))
                    {
                        shouldRemove = true;
                        removedCount++;
                    }
                }
            }

            if (!shouldRemove)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve != null)
                {
                    AnimationUtility.SetEditorCurve(newClip, binding, curve);
                }
            }
        }

        // ObjectReference 커브도 복사 (이벤트, 오브젝트 참조 등)
        EditorCurveBinding[] objBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
        foreach (var binding in objBindings)
        {
            var keyframes = AnimationUtility.GetObjectReferenceCurve(source, binding);
            AnimationUtility.SetObjectReferenceCurve(newClip, binding, keyframes);
        }

        // 이벤트 복사
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(source);
        AnimationUtility.SetAnimationEvents(newClip, events);

        // 설정 적용
        AnimationUtility.SetAnimationClipSettings(newClip, newSettings);

        if (removedCount > 0)
        {
            Debug.Log($"    {clipName}: Position 커브 {removedCount}개 제거됨");
        }
        else
        {
            Debug.Log($"    {clipName}: 제거할 Position 커브 없음 (이미 깨끗함)");
        }

        return newClip;
    }

    /// <summary>
    /// 커브의 값이 의미있게 변하는지 확인.
    /// 시작값과 끝값의 차이가 0.01 이상이면 이동이 있는 것으로 판단.
    /// </summary>
    private static bool HasSignificantMovement(AnimationCurve curve)
    {
        if (curve.keys.Length < 2) return false;

        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (var key in curve.keys)
        {
            if (key.value < min) min = key.value;
            if (key.value > max) max = key.value;
        }

        return (max - min) > 0.01f;
    }

    // ═════════════════════════════════════════════
    // STEP 2: Animator Controller 생성
    // ═════════════════════════════════════════════

    [MenuItem("Tools/Monster/2. Create Animator Controller")]
    public static void CreateMonsterAnimator()
    {
        // ── 추출된 클린 클립 로드 ──
        AnimationClip idleClip   = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CLIP_FOLDER}/Monster_Idle.anim");
        AnimationClip walkClip   = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CLIP_FOLDER}/Monster_Walk.anim");
        AnimationClip runClip    = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CLIP_FOLDER}/Monster_Run.anim");
        AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CLIP_FOLDER}/Monster_Attack.anim");
        AnimationClip hitClip    = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CLIP_FOLDER}/Monster_Hit.anim");
        AnimationClip deathClip  = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{CLIP_FOLDER}/Monster_Death.anim");

        // 누락 체크
        bool anyMissing = false;
        if (idleClip   == null) { Debug.LogError("[Animator] Monster_Idle.anim 없음. Step 1을 먼저 실행하세요!"); anyMissing = true; }
        if (walkClip   == null) { Debug.LogError("[Animator] Monster_Walk.anim 없음."); anyMissing = true; }
        if (runClip    == null) { Debug.LogError("[Animator] Monster_Run.anim 없음."); anyMissing = true; }
        if (attackClip == null) { Debug.LogError("[Animator] Monster_Attack.anim 없음."); anyMissing = true; }
        if (hitClip    == null) { Debug.LogError("[Animator] Monster_Hit.anim 없음."); anyMissing = true; }
        if (deathClip  == null) { Debug.LogError("[Animator] Monster_Death.anim 없음."); anyMissing = true; }

        if (anyMissing)
        {
            Debug.LogError("<color=red>[Animator] 클립이 부족합니다. 'Tools > Monster > 1. Extract and Clean Clips'을 먼저 실행하세요.</color>");
            return;
        }

        // ── Controller 생성 ──
        var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        var sm = controller.layers[0].stateMachine;

        // 파라미터
        controller.AddParameter("Speed",  AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit",    AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        // ── Idle (기본 상태) ──
        var idleState = sm.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        // ── Locomotion (Blend Tree: Walk ↔ Run) ──
        var locoState = sm.AddState("Locomotion", new Vector3(300, 80, 0));
        var blendTree = new BlendTree
        {
            name = "Locomotion",
            blendParameter = "Speed",
            blendType = BlendTreeType.Simple1D,
            useAutomaticThresholds = false
        };
        blendTree.AddChild(walkClip, 0.5f);
        blendTree.AddChild(runClip,  1.0f);
        AssetDatabase.AddObjectToAsset(blendTree, controller);
        locoState.motion = blendTree;

        // ── Attack ──
        var attackState = sm.AddState("Attack", new Vector3(600, 0, 0));
        attackState.motion = attackClip;

        // ── Hit ──
        var hitState = sm.AddState("Hit", new Vector3(600, 80, 0));
        hitState.motion = hitClip;

        // ── Death ──
        var deathState = sm.AddState("Death", new Vector3(600, 160, 0));
        deathState.motion = deathClip;

        // ── 전이 설정 ──
        // Idle ↔ Locomotion
        var t1 = idleState.AddTransition(locoState);
        t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t1.hasExitTime = false; t1.duration = 0.2f;

        var t2 = locoState.AddTransition(idleState);
        t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t2.hasExitTime = false; t2.duration = 0.2f;

        // Any State → Attack
        var ta = sm.AddAnyStateTransition(attackState);
        ta.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        ta.hasExitTime = false; ta.duration = 0.1f; ta.canTransitionToSelf = false;

        var taOut = attackState.AddTransition(idleState);
        taOut.hasExitTime = true; taOut.exitTime = 0.9f; taOut.duration = 0.1f;

        // Any State → Hit
        var th = sm.AddAnyStateTransition(hitState);
        th.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        th.hasExitTime = false; th.duration = 0.05f; th.canTransitionToSelf = false;

        var thOut = hitState.AddTransition(idleState);
        thOut.hasExitTime = true; thOut.exitTime = 0.9f; thOut.duration = 0.1f;

        // Any State → Death
        var td = sm.AddAnyStateTransition(deathState);
        td.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        td.hasExitTime = false; td.duration = 0.1f; td.canTransitionToSelf = false;

        var tdOut = deathState.AddTransition(idleState);
        tdOut.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
        tdOut.hasExitTime = false; tdOut.duration = 0.2f;

        // 저장
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = controller;
        Debug.Log($"<color=green>[Animator] 컨트롤러 생성 완료: {CONTROLLER_PATH}</color>");
    }

    // ═════════════════════════════════════════════
    // 유틸리티
    // ═════════════════════════════════════════════

    private static AnimationClip LoadClipFromFBX(string fbxPath, string clipName)
    {
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (allAssets == null || allAssets.Length == 0)
        {
            Debug.LogError($"FBX를 찾을 수 없습니다: {fbxPath}");
            return null;
        }

        var clips = allAssets
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .ToArray();

        // 정확 매칭
        var match = clips.FirstOrDefault(c => c.name == clipName);
        if (match != null) return match;

        // 부분 매칭
        match = clips.FirstOrDefault(c => c.name.Contains(clipName));
        if (match != null)
        {
            Debug.LogWarning($"'{clipName}' 정확 매칭 실패 → '{match.name}' 사용");
            return match;
        }

        Debug.LogWarning($"'{fbxPath}'에서 '{clipName}' 못 찾음. 사용 가능:");
        foreach (var c in clips) Debug.Log($"  - {c.name}");
        return null;
    }
}
