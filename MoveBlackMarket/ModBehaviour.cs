using System;
using System.Reflection;
using System.Threading.Tasks;
using Duckov.Economy;
using Duckov.Scenes;
using Duckov.UI;
using Duckov.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MoveBlackMarket
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static GameObject? _savedBlueMerchantModel; // 克隆的地毯人模型
        private static GameObject? _savedBlueMerchantShop; // 克隆的地毯人交互
        private static GameObject? _tempBlueMerchantModel; // 临时的地毯人模型
        private static GameObject? _tempBlueMerchantShop; // 临时的地毯人交互

        protected override void OnAfterSetup()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
            SceneLoader.onAfterSceneInitialize += OnAfterSceneInit;
        }

        protected override void OnBeforeDeactivate()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneLoader.onAfterSceneInitialize -= OnAfterSceneInit;
        }

        async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Debug.Log($"加载场景：{scene.name}，模式：{mode.ToString()}");
            if (scene.name == "Base_SceneV2")
            {
                await InitMerchant();
                CreateCharacter("EnemyPreset_Merchant_Myst", new Vector3(7, 0, -51), new Vector3(7, 0, -54));
                CreateCharacter("EnemyPreset_Merchant_Myst0", new Vector3(8, 0, -51), new Vector3(8, 0, -54));
                await AttachBlueMerchantToBase(_savedBlueMerchantModel, _savedBlueMerchantShop,
                    new Vector3(9.5f, 1.5f, -51.5f),
                    new Vector3(9.5f, 1.5f, -54));
            }
        }

        async void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == "Base_SceneV2")
            {
                Destroy(_tempBlueMerchantModel);
                _tempBlueMerchantModel = null;
                Destroy(_tempBlueMerchantShop);
                _tempBlueMerchantShop = null;
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

        public static Task AsTask(AsyncOperation asyncOperation)
        {
            var tcs = new TaskCompletionSource<bool>();

            asyncOperation.completed += (op) =>
            {
                if (op.isDone)
                    tcs.SetResult(true);
            };

            return tcs.Task;
        }

        private static void DisableScene(Scene scene)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                root.SetActive(false);
            }
        }

        private static GameObject? FindRootObjectInScene(Scene scene, string rootName)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                if (root.name == rootName)
                    return root;
            }

            return null;
        }

        private static async Task SceneFindObject(string sourceSceneName, LoadSceneMode loadSceneMode,
            Func<Scene, Task> func)
        {
            Scene sourceScene = SceneManager.GetSceneByName(sourceSceneName);
            bool sceneWasAlreadyLoaded = sourceScene.IsValid() && sourceScene.isLoaded;
            if (!sceneWasAlreadyLoaded)
            {
                // 场景未加载，异步加载场景
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sourceSceneName, loadSceneMode);
                if (asyncLoad == null)
                {
                    Debug.LogError($"无法加载场景: {sourceSceneName}");
                    return;
                }

                asyncLoad.allowSceneActivation = true;
                await AsTask(asyncLoad);
                // 重新获取场景引用
                sourceScene = SceneManager.GetSceneByName(sourceSceneName);
                if (!sourceScene.IsValid())
                {
                    return;
                }

                DisableScene(sourceScene);
            }

            await func(sourceScene);
            // 如果场景是我们加载的，就卸载它；如果是之前就加载的，就保留
            if (!sceneWasAlreadyLoaded)
            {
                //yield return new WaitForEndOfFrame(); // 确保复制完成
                AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sourceSceneName);
                if (asyncUnload == null)
                {
                    Debug.LogError($"无法卸载场景: {sourceSceneName}");
                    return;
                }

                await AsTask(asyncUnload);
            }
        }

        public static GameObject? FindByPathInScene(Scene scene, string hierarchyPathOrName)
        {
            // 路径格式示例: "Canvas/Panel/Button" 或 "RootObject/Child/GrandChild"
            string[] pathParts = hierarchyPathOrName.Split('/');
            if (pathParts.Length == 0)
            {
                return FindRootObjectInScene(scene, hierarchyPathOrName);
            }

            // 首先找到根物体
            GameObject rootObject = FindRootObjectInScene(scene, pathParts[0]);
            if (rootObject == null)
            {
                //Debug.LogError($"在场景 {scene.name} 中找不到根物体: {pathParts[0]}");
                return null;
            }

            // 沿着路径查找
            Transform current = rootObject.transform;
            for (int i = 1; i < pathParts.Length; i++)
            {
                current = current.Find(pathParts[i]);
                if (current == null)
                {
                    //Debug.LogError($"在路径 {hierarchyPathOrName} 中找不到: {pathParts[i]}");
                    return null;
                }
            }

            return current.gameObject;
        }

        private static GameObject? CloneObject(GameObject obj, string name)
        {
            if (obj == null)
                return null;
            GameObject? clone = null;
            clone = Instantiate(obj);
            clone.name = name;
            clone.transform.SetParent(null, true);
            clone.SetActive(false);
            DontDestroyOnLoad(clone);
            return clone;
        }

        private async Task InitMerchant()
        {
            if (_savedBlueMerchantModel == null || _savedBlueMerchantShop == null)
            {
                await SceneFindObject("Level_HiddenWarehouse_CellarUnderGround", LoadSceneMode.Additive, (scene) =>
                {
                    if (_savedBlueMerchantModel == null)
                    {
                        var obj = FindByPathInScene(scene, "ENV/Inside/Group/bugboss_patro_stand_2");
                        if (obj == null)
                        {
                            Debug.Log("未找到地摊人模型");
                        }
                        else
                        {
                            _savedBlueMerchantModel = CloneObject(obj.gameObject, "地摊人模型");
                            //Debug.Log("已克隆并地摊人模型");
                        }
                    }

                    if (_savedBlueMerchantShop == null)
                    {
                        var obj = FindByPathInScene(scene, "ENV/Inside/Group/Shop");
                        if (obj == null)
                        {
                            Debug.Log("未找到地摊人交互对象");
                        }
                        else
                        {
                            _savedBlueMerchantShop = CloneObject(obj.gameObject, "地摊人商店交互");
                            //Debug.Log("已克隆并地摊人交互对象");
                        }
                    }

                    return Task.CompletedTask;
                });
            }
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

        public static void RefreshShop(StockShop stockShop)
        {
            if (stockShop == null)
            {
                Debug.LogWarning("stockShop == null");
                return;
            }

            // 反射调用DoRefreshStock
            var refreshMethod = typeof(StockShop).GetMethod("DoRefreshStock",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (refreshMethod == null)
            {
                Debug.LogWarning("未找到DoRefreshStock");
                return;
            }

            try
            {
                refreshMethod.Invoke(stockShop, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"调用DoRefreshStock时发生异常: {ex.Message}");
            }

            //反射设置lastTimeRefreshedStock
            var lastTimeField = typeof(StockShop).GetField("lastTimeRefreshedStock",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (lastTimeField == null)
            {
                Debug.LogWarning("未找到lastTimeRefreshedStock");
                return;
            }

            try
            {
                lastTimeField.SetValue(stockShop, DateTime.UtcNow.ToBinary());
            }
            catch (Exception ex)
            {
                Debug.LogError($"设置lastTimeRefreshedStock时发生异常: {ex.Message}");
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

                var merchantChild = GetSpecialMerchantChild(character.transform);
                if (merchantChild == null)
                {
                    Debug.LogWarning("未找到SpecialAttachment_Merchant");
                    return;
                }

                var stockShop = merchantChild.GetComponent<StockShop>();
                if (stockShop == null)
                {
                    Debug.LogWarning("未找到StockShop组件");
                    return;
                }

                // 绑定受伤事件
                var health = character.GetComponent<Health>();
                if (health != null)
                {
                    void OnMerchantHurtEvent(DamageInfo damageInfo)
                    {
                        RefreshShop(stockShop);
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

        async Task AttachBlueMerchantToBase(GameObject? model, GameObject? shop, Vector3 position, Vector3 faceTo,
            float waitfor = 1f)
        {
            await Task.Delay(TimeSpan.FromSeconds(waitfor));
            if (model == null)
            {
                //Debug.LogWarning("没有保存的地摊人模型");
                return;
            }

            if (shop == null)
            {
                //Debug.LogWarning("没有保存的地摊人商店");
                return;
            }

            var baseRoot = GameObject.Find("MultiSceneCore/Base_SceneV2/");
            if (baseRoot == null)
            {
                Debug.LogWarning("未找到 Base_SceneV2 根节点");
                return;
            }

            if (_tempBlueMerchantModel != null && _tempBlueMerchantShop != null)
            {
                return;
            }

            _tempBlueMerchantModel = Instantiate(model);
            _tempBlueMerchantShop = Instantiate(shop);

            _tempBlueMerchantModel.transform.SetParent(baseRoot.transform, true);
            _tempBlueMerchantShop.transform.SetParent(baseRoot.transform, true);
            _tempBlueMerchantModel.transform.position = position;
            // _tempBlueMerchantShop.transform.position = new Vector3(position.x + 0.3f, position.y + 0.4f, position.z);
            _tempBlueMerchantShop.transform.position = new Vector3(position.x + 0.3f, 0f, position.z);

            // 设置地摊人朝向
            _tempBlueMerchantModel.transform.LookAt(faceTo);
            //Debug.Log($"地摊人朝向已设置: {faceTo}");

            //添加碰撞器
            CapsuleCollider collider = _tempBlueMerchantModel.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 0.5f, 0);
            collider.direction = 1;
            collider.height = 1f;
            collider.radius = 0.05f;
            _tempBlueMerchantModel.layer = GameplayDataSettings.Layers.damageReceiverLayerMask;

            var stockShop = _tempBlueMerchantShop.GetComponent<StockShop>();
            if (stockShop == null)
            {
                Debug.LogWarning("未找到StockShop组件");
                return;
            }

            DamageReceiver damageReceiver = _tempBlueMerchantModel.AddComponent<DamageReceiver>();
            if (damageReceiver == null)
            {
                Debug.LogWarning("未找到DamageReceiver组件");
                return;
            }

            //damageReceiver.useSimpleHealth = false;
            if (damageReceiver.OnHurtEvent == null)
            {
                //创建OnHurtEvent事件
                damageReceiver.OnHurtEvent = new UnityEvent<DamageInfo>();
            }

            damageReceiver.OnHurtEvent.AddListener((damageInfo) =>
            {
                // 刷新商店物品            
                RefreshShop(stockShop);
                NotificationText.Push($"商店已刷新");
            });
            _tempBlueMerchantModel.SetActive(true);
            _tempBlueMerchantShop.SetActive(true);
            //Debug.Log($"地摊人已激活");
        }
    }
}