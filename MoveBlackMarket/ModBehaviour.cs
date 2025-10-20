using System;
using System.Collections;
using UnityEngine;

namespace MoveBlackMarket
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static GameObject? _savedMerchant; // 保存克隆的商人

        protected override void OnAfterSetup()
        {
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
            SceneLoader.onAfterSceneInitialize += OnAfterSceneInit;
            SceneLoader.onStartedLoadingScene -= OnStartedLoadingScene;
            SceneLoader.onStartedLoadingScene += OnStartedLoadingScene;
        }

        protected override void OnBeforeDeactivate()
        {
            Destroy(_savedMerchant);
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
            SceneLoader.onStartedLoadingScene -= OnStartedLoadingScene;
        }

        void OnAfterSceneInit(SceneLoadingContext context)
        {
            Debug.Log($"场景加载完成: {context.sceneName}");

            if (context.sceneName == "Level_GroundZero_Main" && _savedMerchant == null)
                StartCoroutine(FindAndCloneMerchant());
            else if (context.sceneName == "Base")
                StartCoroutine(AttachMerchantToBase());
        }

        void OnStartedLoadingScene(SceneLoadingContext context)
        {
            Debug.Log($"开始加载场景: {context.sceneName}");
            if (context.sceneName != "Base" && _savedMerchant != null)
                _savedMerchant.SetActive(false);
        }

        void OnBeforeMerchantDead(DamageInfo damage)
        {
            Destroy(_savedMerchant);
        }

        IEnumerator FindAndCloneMerchant()
        {
            Debug.Log("延迟5秒等待场景生成...");
            yield return new WaitForSeconds(5f);

            var root = GameObject.Find("MultiSceneCore/Level_GroundZero_1");
            if (root == null)
            {
                Debug.LogWarning("未找到根节点 MultiSceneCore/Level_GroundZero_1，打印所有根节点:");
                foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    Debug.Log("Root: " + go.name);
                }

                yield break;
            }

            Debug.Log("找到根节点: " + root.name);

            bool foundMerchant = false;
            foreach (Transform child in root.transform)
            {
                Debug.Log("遍历子对象: " + child.name);
                if (child.name == "Character(Clone)")
                {
                    var ctrl = child.GetComponent<CharacterMainControl>();
                    if (ctrl != null)
                    {
                        Debug.Log($"CharacterMainControl.Team = {ctrl.Team}");
                        ctrl.BeforeCharacterSpawnLootOnDead -= OnBeforeMerchantDead;
                        ctrl.BeforeCharacterSpawnLootOnDead += OnBeforeMerchantDead;
                    }

                    if (ctrl != null && ctrl.Team == Teams.all && HasSpecialMerchantChild(child))
                    {
                        Debug.Log($"✅ 找到商人: {child.name} @ {child.position}");

                        if (_savedMerchant != null)
                            Destroy(_savedMerchant);

                        var clone = Instantiate(child.gameObject);
                        clone.name = "黑市商人的兄弟";
                        clone.transform.SetParent(null, true);
                        clone.SetActive(false);

                        // 禁用 AIController_Merchant_Myst(Clone)
                        var aiChild = clone.transform.Find("AIController_Merchant_Myst(Clone)");
                        if (aiChild != null)
                        {
                            aiChild.gameObject.SetActive(false);
                            Debug.Log("✅ 已禁用 AIController_Merchant_Myst(Clone)");
                        }
                        else
                        {
                            Debug.LogWarning("❌ 未找到 AIController_Merchant_Myst(Clone)");
                        }

                        _savedMerchant = clone;
                        DontDestroyOnLoad(_savedMerchant);
                        Debug.Log("✅ 已克隆并保存商人对象（DontDestroyOnLoad生效）");

                        foundMerchant = true;
                        break;
                    }
                }
            }

            if (!foundMerchant)
            {
                Debug.LogWarning("❌ 没有找到符合条件的商人对象");
            }
            else
            {
                LevelManager.Instance.MainCharacter.PopText("商人的兄弟已被请回基地");
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
            yield return new WaitForSeconds(1f);

            if (_savedMerchant == null)
            {
                Debug.LogWarning("❌ 没有保存的商人对象");
                yield break;
            }

            // var baseRoot = GameObject.Find("MultiSceneCore/Base");
            // if (baseRoot == null)
            // {
            //     Debug.LogWarning("❌ 未找到 Base 根节点");
            //     yield break;
            // }

            //// 挂载到 Base
            // _savedMerchant.transform.SetParent(baseRoot.transform, true);
            _savedMerchant.transform.SetParent(null, true);
            _savedMerchant.transform.position = new Vector3(7, 0, -51);

            // 设置商人朝向
            var modelRoot = _savedMerchant.transform.Find("ModelRoot");
            if (modelRoot != null)
            {
                modelRoot.LookAt(new Vector3(7, 0, -54));
                Debug.Log("✅ 商人朝向已设置: ModelRoot.LookAt(7,0,-54)");
            }
            else
            {
                Debug.LogWarning("❌ 未找到 ModelRoot，无法设置朝向");
            }

            _savedMerchant.SetActive(true);
            Debug.Log($"✅ 商人已激活");
        }
    }
}