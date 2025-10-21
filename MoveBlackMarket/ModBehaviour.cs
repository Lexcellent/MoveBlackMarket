using System;
using System.Collections;
using System.Reflection;
using Duckov.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoveBlackMarket
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static GameObject? _savedZeroMerchant; // 克隆的零号区商人
        private static GameObject? _savedFarmMerchant; // 克隆的农场商人

        protected override void OnAfterSetup()
        {
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
            SceneLoader.onAfterSceneInitialize += OnAfterSceneInit;
            SceneLoader.onStartedLoadingScene -= OnStartedLoadingScene;
            SceneLoader.onStartedLoadingScene += OnStartedLoadingScene;
        }

        protected override void OnBeforeDeactivate()
        {
            if (_savedZeroMerchant != null)
            {
                Destroy(_savedZeroMerchant);
            }

            if (_savedFarmMerchant != null)
            {
                Destroy(_savedFarmMerchant);
            }

            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
            SceneLoader.onStartedLoadingScene -= OnStartedLoadingScene;
        }

        void OnAfterSceneInit(SceneLoadingContext context)
        {
            Debug.Log($"场景加载完成: {context.sceneName}");

            if (context.sceneName == "Level_GroundZero_Main" && _savedZeroMerchant == null)
                StartCoroutine(FindAndCloneMerchant("MultiSceneCore/Level_GroundZero_1",
                    clone => _savedZeroMerchant = clone));
            if (context.sceneName == "Level_Farm_Main" && _savedFarmMerchant == null)
                StartCoroutine(FindAndCloneMerchant("MultiSceneCore/Level_Farm_01/",
                    clone => _savedFarmMerchant = clone));
            else if (context.sceneName == "Base")
            {
                StartCoroutine(AttachMerchantToBase(_savedZeroMerchant, new Vector3(7, 0, -51),
                    new Vector3(7, 0, -54)));
                StartCoroutine(AttachMerchantToBase(_savedFarmMerchant, new Vector3(8, 0, -51),
                    new Vector3(8, 0, -54)));
            }
        }

        void OnStartedLoadingScene(SceneLoadingContext context)
        {
            Debug.Log($"开始加载场景: {context.sceneName}");
            if (context.sceneName != "Base")
            {
                _savedZeroMerchant?.SetActive(false);
                _savedFarmMerchant?.SetActive(false);
            }
        }

        void OnBeforeMerchantDead(DamageInfo damage)
        {
            if (_savedZeroMerchant != null)
            {
                Destroy(_savedZeroMerchant);
            }

            if (_savedFarmMerchant != null)
            {
                Destroy(_savedFarmMerchant);
            }
        }

        IEnumerator FindAndCloneMerchant(string rootPath, Action<GameObject> onCloned)
        {
            Debug.Log("延迟5秒等待场景生成...");
            yield return new WaitForSeconds(5f);

            var root = GameObject.Find(rootPath);
            if (root == null)
            {
                Debug.LogWarning($"未找到根节点 {rootPath}，打印所有根节点:");
                foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    Debug.Log("Root: " + go.name);
                }

                yield break;
            }

            // Debug.Log("找到根节点: " + root.name);

            foreach (Transform child in root.transform)
            {
                // Debug.Log("遍历子对象: " + child.name);
                if (child.name == "Character(Clone)")
                {
                    var ctrl = child.GetComponent<CharacterMainControl>();
                    if (ctrl != null)
                    {
                        Debug.Log($"CharacterMainControl.Team = {ctrl.Team}");
                    }

                    if (ctrl != null && ctrl.Team == Teams.all && HasSpecialMerchantChild(child))
                    {
                        Debug.Log($"✅ 找到商人: {child.name} @ {child.position}");

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

                        DontDestroyOnLoad(clone);
                        Debug.Log("✅ 已克隆并保存商人对象（DontDestroyOnLoad生效）");
                        LevelManager.Instance.MainCharacter.PopText("本地图黑市商人的兄弟已被请回基地");
                        onCloned?.Invoke(clone);
                        yield break;
                    }
                }
            }

            Debug.LogWarning("❌ 没有找到符合条件的商人对象");
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
        Transform? GetSpecialMerchantChild(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith("SpecialAttachment_Merchant_"))
                    return child;
            }

            return null;
        }

        IEnumerator AttachMerchantToBase(GameObject? savedMerchant, Vector3 position, Vector3 faceTo,
            float waitfor = 2f)
        {
            yield return new WaitForSeconds(waitfor);

            if (savedMerchant == null)
            {
                Debug.LogWarning("❌ 没有保存的商人对象");
                yield break;
            }

            var cloneMerchant = Instantiate(savedMerchant);

            var baseRoot = GameObject.Find("MultiSceneCore/Base");
            if (baseRoot == null)
            {
                Debug.LogWarning("❌ 未找到 Base 根节点");
                yield break;
            }

            cloneMerchant.transform.SetParent(baseRoot.transform, true);
            cloneMerchant.transform.position = position;

            // 设置商人朝向
            var modelRoot = cloneMerchant.transform.Find("ModelRoot");
            if (modelRoot != null)
            {
                modelRoot.LookAt(faceTo);
                Debug.Log($"✅ 商人朝向已设置: {faceTo}");
            }

            // 直接监听 DamageReceiver 的 OnDeadEvent
            var damageReceiver = cloneMerchant.GetComponentInChildren<DamageReceiver>();
            if (damageReceiver != null)
            {
                damageReceiver.OnDeadEvent.AddListener((damageInfo) =>
                {
                    Debug.Log($"🔄 商人死亡，准备复活: {cloneMerchant.name}");
                    // 延迟复活
                    StartCoroutine(RespawnMerchantAfterDeath(savedMerchant, position, faceTo, 1f));
                });
                Debug.Log("✅ 已绑定商人死亡监听事件");
            }
            else
            {
                Debug.LogWarning("❌ 未找到 DamageReceiver 组件");
            }

            
            cloneMerchant.SetActive(true);
            Debug.Log($"✅ 商人已激活");
            // 刷新商店物品
            var find = GetSpecialMerchantChild(cloneMerchant.transform);
            if (find != null)
            {
                var stockShop = find.GetComponent<StockShop>();
                if (stockShop != null)
                {
                    Debug.Log($"🔍 尝试刷新商人商店库存...");
                    // // 使用反射调用 InitializeEntries 方法
                    // var initializeEntriesMethod = typeof(StockShop).GetMethod("InitializeEntries",
                    //     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    // if (initializeEntriesMethod != null)
                    // {
                    //     try
                    //     {
                    //         initializeEntriesMethod.Invoke(stockShop, null);
                    //         Debug.Log($"✅ 成功调用 InitializeEntries 方法，商店库存已刷新");
                    //     }
                    //     catch (Exception ex)
                    //     {
                    //         Debug.LogError($"❌ 调用 InitializeEntries 方法时发生异常: {ex.Message}");
                    //     }
                    // }
                    // else
                    // {
                    //     Debug.LogWarning("⚠️ 未找到 InitializeEntries 方法");
                    // }

                    // 使用反射调用 DoRefreshStock 方法
                    var refreshMethod = typeof(StockShop).GetMethod("DoRefreshStock",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (refreshMethod != null)
                    {
                        try
                        {
                            refreshMethod.Invoke(stockShop, null);
                            Debug.Log($"✅ 成功调用 DoRefreshStock 方法，商店库存已刷新");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"❌ 调用 DoRefreshStock 方法时发生异常: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ 未找到 DoRefreshStock 方法");
                    }

                    // 使用反射设置 lastTimeRefreshedStock 字段
                    var lastTimeField = typeof(StockShop).GetField("lastTimeRefreshedStock",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (lastTimeField != null)
                    {
                        try
                        {
                            lastTimeField.SetValue(stockShop, DateTime.UtcNow.ToBinary());
                            Debug.Log($"✅ 成功更新 lastTimeRefreshedStock 时间戳");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"❌ 设置 lastTimeRefreshedStock 字段时发生异常: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ 未找到 lastTimeRefreshedStock 字段");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ SpecialAttachment_Merchant_ 上未找到 StockShop 组件");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到 SpecialAttachment_Merchant_ 对象");
            }

        }

        // 商人复活协程
        IEnumerator RespawnMerchantAfterDeath(GameObject savedMerchant, Vector3 position, Vector3 faceTo,
            float delaySeconds)
        {
            Debug.Log($"⏳ 等待 {delaySeconds} 秒后复活商人...");
            yield return new WaitForSeconds(delaySeconds);

            StartCoroutine(AttachMerchantToBase(savedMerchant, position, faceTo, 0f));
        }
    }
}