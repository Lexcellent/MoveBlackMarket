using System.Collections;
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

        IEnumerator FindAndCloneMerchant(string rootPath, System.Action<GameObject> onCloned)
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

            Debug.Log("找到根节点: " + root.name);

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

        IEnumerator AttachMerchantToBase(GameObject? savedMerchant, Vector3 position, Vector3 faceTo)
        {
            yield return new WaitForSeconds(1f);

            if (savedMerchant == null)
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
            savedMerchant.transform.SetParent(null, true);
            savedMerchant.transform.position = position;

            // 设置商人朝向
            var modelRoot = savedMerchant.transform.Find("ModelRoot");
            if (modelRoot != null)
            {
                modelRoot.LookAt(faceTo);
                Debug.Log($"✅ 商人朝向已设置: {faceTo}");
            }
            else
            {
                Debug.LogWarning("❌ 未找到 ModelRoot，无法设置朝向");
            }

            savedMerchant.SetActive(true);
            Debug.Log($"✅ 商人已激活");
        }
    }
}