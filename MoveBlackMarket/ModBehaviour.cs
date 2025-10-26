using System;
using System.Collections;
using System.Reflection;
using Duckov.Economy;
using Duckov.Scenes;
using Duckov.UI;
using Duckov.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoveBlackMarket
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static GameObject? _savedBlueMerchantModel; // 克隆的地毯人模型
        private static GameObject? _savedBlueMerchantShop; // 克隆的地毯人交互

        protected override void OnAfterSetup()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
            SceneLoader.onAfterSceneInitialize += OnAfterSceneInit;
        }

        protected override void OnBeforeDeactivate()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
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
            else if (scene.name == "Base_SceneV2")
            {
                CreateCharacter("EnemyPreset_Merchant_Myst", new Vector3(7, 0, -51), new Vector3(7, 0, -54));
                CreateCharacter("EnemyPreset_Merchant_Myst0", new Vector3(8, 0, -51), new Vector3(8, 0, -54));
                StartCoroutine(AttachBlueMerchantToBase(_savedBlueMerchantModel, _savedBlueMerchantShop,
                    new Vector3(6, 0, -51),
                    new Vector3(6, 0, -54)));
            }
        }

        void OnAfterSceneInit(SceneLoadingContext context)
        {
            // Debug.Log($"场景初始化完成：{context.sceneName}");
            // if (context.sceneName == "Base")
            // {
            //     CreateCharacter("EnemyPreset_Merchant_Myst", new Vector3(7, 0, -51), new Vector3(7, 0, -54));
            //     CreateCharacter("EnemyPreset_Merchant_Myst0", new Vector3(8, 0, -51), new Vector3(8, 0, -54));
            //     // StartCoroutine(AttachMerchantToBase(_savedZeroMerchant, new Vector3(7, 0, -51),
            //     //     new Vector3(7, 0, -54)));
            //     // StartCoroutine(AttachMerchantToBase(_savedFarmMerchant, new Vector3(8, 0, -51),
            //     //     new Vector3(8, 0, -54)));
            //     StartCoroutine(AttachBlueMerchantToBase(_savedBlueMerchantModel, _savedBlueMerchantShop,
            //         new Vector3(6, 0, -51),
            //         new Vector3(6, 0, -54)));
            // }
        }


        IEnumerator CopyMerchantBlue(Action<GameObject, GameObject> onCloned)
        {
            // Debug.Log("延迟5秒等待场景生成...");
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
                    // Debug.Log($"✅ 找到地毯人模型: {child.name} @ {child.position}");

                    model = Instantiate(child.gameObject);
                    model.name = "地毯人模型";
                    model.transform.SetParent(null, true);
                    model.SetActive(false);
                    DontDestroyOnLoad(model);
                    // Debug.Log("✅ 已克隆并地毯人模型（DontDestroyOnLoad生效）");
                }
                else if (child.name == "Shop")
                {
                    // Debug.Log($"✅ 找到地毯人交互对象: {child.name} @ {child.position}");

                    shop = Instantiate(child.gameObject);
                    shop.name = "地毯人商店交互";
                    shop.transform.SetParent(null, true);
                    shop.SetActive(false);
                    DontDestroyOnLoad(shop);
                    // Debug.Log("✅ 已克隆并地毯人交互对象（DontDestroyOnLoad生效）");
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

        void RefreshShop(CharacterMainControl character)
        {
            // 刷新商店物品
            var find = GetSpecialMerchantChild(character.transform);
            if (find != null)
            {
                var stockShop = find.GetComponent<StockShop>();
                if (stockShop != null)
                {
                    // Debug.Log($"🔍 尝试刷新商人商店库存...");

                    // 使用反射调用 DoRefreshStock 方法
                    var refreshMethod = typeof(StockShop).GetMethod("DoRefreshStock",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (refreshMethod != null)
                    {
                        try
                        {
                            refreshMethod.Invoke(stockShop, null);
                            // Debug.Log($"✅ 成功调用 DoRefreshStock 方法，商店库存已刷新");
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
                            // Debug.Log($"✅ 成功更新 lastTimeRefreshedStock 时间戳");
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

        CharacterRandomPreset? GetCharacterPreset(string characterPresetName)
        {
            // Debug.Log($"要找的NPC预设:{characterPresetName}");
            foreach (var characterRandomPreset in GameplayDataSettings.CharacterRandomPresetData.presets)
            {
                // Debug.Log($"{characterRandomPreset.name}:{characterRandomPreset.DisplayName}");
                if (characterPresetName == characterRandomPreset.name)
                {
                    return characterRandomPreset;
                }
            }

            return null;
        }


        async void CreateCharacter(string characterPresetName, Vector3 position, Vector3 faceTo)
        {
            // AICharacterController,AISpecialAttachment_Shop,StockShop,Health
            var characterRandomPreset = GetCharacterPreset(characterPresetName);
            if (characterRandomPreset == null)
            {
                return;
            }

            // 创建角色
            var character = await characterRandomPreset.CreateCharacterAsync(position, faceTo,
                MultiSceneCore.MainScene.Value.buildIndex, (CharacterSpawnerGroup)null, false);
            if (character != null)
            {
                // 禁用AI反击
                var aiChild = character.transform.Find("AIController_Merchant_Myst(Clone)");
                if (aiChild != null)
                {
                    var aiCharacterController = aiChild.GetComponent<AICharacterController>();
                    if (aiCharacterController != null)
                    {
                        // aiCharacterController.alertTree = null;
                        aiCharacterController.combat_Attack_Tree = null;
                        aiCharacterController.combatTree = null;
                        aiCharacterController.patrolTree = null;
                        // Debug.Log("AI反击已禁用");
                    }
                }

                // 绑定受伤事件
                var health = character.GetComponent<Health>();
                if (health != null)
                {
                    void OnMerchantHurtEvent(DamageInfo damageInfo)
                    {
                        RefreshShop(character);
                        NotificationText.Push($"商店已刷新");
                    }

                    health.OnHurtEvent.AddListener(OnMerchantHurtEvent);
                }


                var baseRoot = GameObject.Find("MultiSceneCore/Base");
                if (baseRoot != null)
                {
                    character.transform.SetParent(baseRoot.transform, true);
                }
            }
        }

        IEnumerator AttachBlueMerchantToBase(GameObject? model, GameObject? shop, Vector3 position, Vector3 faceTo,
            float waitfor = 2f)
        {
            yield return new WaitForSeconds(waitfor);

            if (model == null)
            {
                // Debug.LogWarning("❌ 没有保存的地毯人模型");
                yield break;
            }

            if (shop == null)
            {
                // Debug.LogWarning("❌ 没有保存的地毯人商店");
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
                // Debug.Log($"✅ 地毯人朝向已设置: {faceTo}");
            }

            cloneModel.SetActive(true);
            cloneShop.SetActive(true);
            // Debug.Log($"✅ 商人已激活");
            // 刷新商店物品
            var stockShop = cloneShop.GetComponent<StockShop>();
            if (stockShop != null)
            {
                // Debug.Log($"🔍 尝试刷新商人商店库存...");
                // 使用反射调用 DoRefreshStock 方法
                var refreshMethod = typeof(StockShop).GetMethod("DoRefreshStock",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (refreshMethod != null)
                {
                    try
                    {
                        refreshMethod.Invoke(stockShop, null);
                        // Debug.Log($"✅ 成功调用 DoRefreshStock 方法，商店库存已刷新");
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
                        // Debug.Log($"✅ 成功更新 lastTimeRefreshedStock 时间戳");
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
    }
}