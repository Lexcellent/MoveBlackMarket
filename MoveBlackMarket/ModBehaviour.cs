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
        private static GameObject? _savedBlueMerchantModel; // 克隆的地毯人模型
        private static GameObject? _savedBlueMerchantShop; // 克隆的地毯人交互

        protected override void OnAfterSetup()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
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

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
            SceneLoader.onStartedLoadingScene -= OnStartedLoadingScene;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"加载场景：{scene.name}，模式：{mode.ToString()}");
            if (scene.name == "Level_HiddenWarehouse_CellarUnderGround")
            {
                // 加载了隐藏仓库地下室场景
                if (_savedBlueMerchantModel == null || _savedBlueMerchantShop == null)
                {
                    // 延迟执行商人的复制
                    StartCoroutine(CopyMerchantBlue((model, shop) =>
                    {
                        _savedBlueMerchantModel = model;
                        _savedBlueMerchantShop = shop;
                    }));
                }
            }
        }

        IEnumerator CopyMerchantBlue(Action<GameObject, GameObject> onCloned)
        {
            Debug.Log("延迟5秒等待场景生成...");
            yield return new WaitForSeconds(5f);

            var root = GameObject.Find("ENV/Inside/Group");
            if (root == null)
            {
                Debug.LogWarning($"未找到根节点 ENV/Inside/Group，打印所有根节点:");
                // foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                // {
                //     Debug.Log("Root: " + go.name);
                // }

                yield break;
            }

            // Debug.Log("找到根节点: " + root.name);
            GameObject? model = null;
            GameObject? shop = null;
            foreach (Transform child in root.transform)
            {
                // Debug.Log("遍历子对象: " + child.name);
                if (child.name == "bugboss_patro_stand_2")
                {
                    Debug.Log($"✅ 找到地毯人模型: {child.name} @ {child.position}");

                    model = Instantiate(child.gameObject);
                    model.name = "地毯人模型";
                    model.transform.SetParent(null, true);
                    model.SetActive(false);
                    DontDestroyOnLoad(model);
                    Debug.Log("✅ 已克隆并地毯人模型（DontDestroyOnLoad生效）");
                }
                else if (child.name == "Shop")
                {
                    Debug.Log($"✅ 找到地毯人交互对象: {child.name} @ {child.position}");

                    shop = Instantiate(child.gameObject);
                    shop.name = "地毯人商店交互";
                    shop.transform.SetParent(null, true);
                    shop.SetActive(false);
                    DontDestroyOnLoad(shop);
                    Debug.Log("✅ 已克隆并地毯人交互对象（DontDestroyOnLoad生效）");
                }

                if (model != null && shop != null)
                {
                    break;
                }
            }

            if (model != null && shop != null)
            {
                LevelManager.Instance.MainCharacter.PopText("地毯人的兄弟已被请回基地");
                onCloned?.Invoke(model, shop);
            }
            else
            {
                Debug.LogWarning("❌ 没有找到地毯人");
            }
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
                StartCoroutine(AttachBlueMerchantToBase(_savedBlueMerchantModel, _savedBlueMerchantShop,
                    new Vector3(6, 0, -51),
                    new Vector3(6, 0, -54)));
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

        IEnumerator FindAndCloneMerchant(string rootPath, Action<GameObject> onCloned)
        {
            Debug.Log("延迟5秒等待场景生成...");
            yield return new WaitForSeconds(5f);

            var root = GameObject.Find(rootPath);
            if (root == null)
            {
                Debug.LogWarning($"未找到根节点 {rootPath}，打印所有根节点:");
                // foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                // {
                //     Debug.Log("Root: " + go.name);
                // }

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

        IEnumerator AttachBlueMerchantToBase(GameObject? model, GameObject? shop, Vector3 position, Vector3 faceTo,
            float waitfor = 2f)
        {
            yield return new WaitForSeconds(waitfor);

            if (model == null)
            {
                Debug.LogWarning("❌ 没有保存的地毯人模型");
                yield break;
            }

            if (shop == null)
            {
                Debug.LogWarning("❌ 没有保存的地毯人商店");
                yield break;
            }

            var baseRoot = GameObject.Find("MultiSceneCore/Base");
            if (baseRoot == null)
            {
                Debug.LogWarning("❌ 未找到 Base 根节点");
                yield break;
            }

            var cloneModel = Instantiate(model);
            var cloneShop = Instantiate(shop);

            cloneModel.transform.SetParent(baseRoot.transform, true);
            cloneShop.transform.SetParent(baseRoot.transform, true);
            cloneModel.transform.position = position;
            cloneShop.transform.position = new Vector3(position.x + 0.3f, position.y + 0.4f, position.z);

            // 设置地毯人朝向
            var modelRoot = cloneModel.transform.Find("ModelRoot");
            if (modelRoot != null)
            {
                modelRoot.LookAt(faceTo);
                Debug.Log($"✅ 地毯人朝向已设置: {faceTo}");
            }

            // 直接监听 DamageReceiver 的 OnDeadEvent
            var damageReceiver = cloneModel.GetComponentInChildren<DamageReceiver>();
            if (damageReceiver != null)
            {
                damageReceiver.OnDeadEvent.AddListener((damageInfo) =>
                {
                    Debug.Log($"🔄 商人死亡，准备复活: {cloneModel.name}");
                    // 延迟复活
                    StartCoroutine(RespawnBlueMerchantAfterDeath(model, shop, position, faceTo, 1f));
                });
                Debug.Log("✅ 已绑定商人死亡监听事件");
            }
            else
            {
                Debug.LogWarning("❌ 未找到 DamageReceiver 组件");
            }


            cloneModel.SetActive(true);
            cloneShop.SetActive(true);
            Debug.Log($"✅ 商人已激活");
            // 刷新商店物品
            var stockShop = cloneShop.GetComponent<StockShop>();
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

        // 地毯人复活协程
        IEnumerator RespawnBlueMerchantAfterDeath(GameObject model, GameObject shop, Vector3 position, Vector3 faceTo,
            float delaySeconds)
        {
            Debug.Log($"⏳ 等待 {delaySeconds} 秒后复活地毯人...");
            yield return new WaitForSeconds(delaySeconds);

            StartCoroutine(AttachBlueMerchantToBase(model, shop, position, faceTo, 0f));
        }
    }
}