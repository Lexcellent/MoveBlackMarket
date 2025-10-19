using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace MoveBlackMarket
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static GameObject? _savedMerchant;

        protected override void OnAfterSetup()
        {
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
            SceneLoader.onAfterSceneInitialize += OnAfterSceneInit;
        }

        protected override void OnBeforeDeactivate()
        {
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
        }

        void OnAfterSceneInit(SceneLoadingContext context)
        {
            Debug.Log($"场景加载完成: {context.sceneName}");

            if (context.sceneName == "Level_GroundZero_Main")
                StartCoroutine(FindAndCloneMerchant());
            else if (context.sceneName == "Base")
                StartCoroutine(AttachMerchantToBase());
        }

        IEnumerator FindAndCloneMerchant()
        {
            yield return new WaitForSeconds(5f);

            var root = GameObject.Find("MultiSceneCore/Level_GroundZero_1");
            if (root == null)
            {
                Debug.LogWarning("未找到根节点 MultiSceneCore/Level_GroundZero_1");
                yield break;
            }

            foreach (Transform child in root.transform)
            {
                if (child.name == "Character(Clone)")
                {
                    var ctrl = child.GetComponent<CharacterMainControl>();
                    if (ctrl != null && ctrl.Team == Teams.all && HasSpecialMerchantChild(child))
                    {
                        Debug.Log($"✅ 找到商人: {child.name} @ {child.position}");

                        if (_savedMerchant != null)
                            UnityEngine.Object.Destroy(_savedMerchant);

                        var clone = UnityEngine.Object.Instantiate(child.gameObject);
                        clone.name = "Merchant(Clone_Copy)";
                        clone.transform.SetParent(null, true);
                        clone.SetActive(false);

                        // 禁用特定子物体，避免跨场景报错
                        var aiChild = clone.transform.Find("AIController_Merchant_Myst(Clone)");
                        if (aiChild != null)
                            aiChild.gameObject.SetActive(false);

                        UnityEngine.Object.DontDestroyOnLoad(clone);
                        _savedMerchant = clone;

                        Debug.Log("✅ 已克隆并保存商人对象（DontDestroyOnLoad生效）");
                        break;
                    }
                }
            }
        }

        bool HasSpecialMerchantChild(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith("SpecialAttachment_Merchant_"))
                    return true;
            }
            return false;
        }

        IEnumerator AttachMerchantToBase()
        {
            yield return new WaitForSeconds(5f);

            if (_savedMerchant == null)
            {
                Debug.LogWarning("❌ 没有保存的商人对象");
                yield break;
            }

            var baseRoot = GameObject.Find("MultiSceneCore/Base");
            if (baseRoot == null)
            {
                Debug.LogWarning("❌ 未找到 Base 根节点");
                yield break;
            }

            // 挂载到 Base
            _savedMerchant.transform.SetParent(baseRoot.transform, true);
            _savedMerchant.transform.position = new Vector3(7, 0, -51);

            // 启用 Movement / CharacterMainControl 保持转向逻辑
            foreach (var comp in _savedMerchant.GetComponents<MonoBehaviour>())
            {
                if (comp != null && (comp is Movement || comp is CharacterMainControl))
                    comp.enabled = true;
            }

            _savedMerchant.SetActive(true);
            Debug.Log($"✅ 商人已挂载到 {baseRoot.name} 并激活");
        }
    }
}
