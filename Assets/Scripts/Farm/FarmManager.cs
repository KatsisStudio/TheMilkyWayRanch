﻿using CowMilking.DialogueSystem;
using CowMilking.Farm.Upgrade;
using CowMilking.Persistency;
using CowMilking.Questing;
using CowMilking.SO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CowMilking.Farm
{
    public class FarmManager : MonoBehaviour
    {
        public static FarmManager Instance { private set; get; }

        [SerializeField]
        public GameObject[] _buttons;
        public IUpdatableUI[] _updatables;

        [SerializeField]
        private GameObject _preventButtonInteractions;

        [SerializeField]
        private GameObject _cowPrefab;

        [SerializeField]
        private Transform _upgradeContainer;

        [SerializeField]
        private GameObject _upgradePrefab;

        [SerializeField]
        private GameObject _milkingPreview;

        [SerializeField]
        private Transform _milkingCam;

        private int _cowLayer;

        public List<FarmCowController> Cows { get; } = new();

        public ClickAction ClickAction { set; private get; }
        private FarmCowController _selectedCow;

        private Camera _cam;

        private void Awake()
        {
            Instance = this;
            SceneManager.LoadScene("CowManager", LoadSceneMode.Additive);
            SceneManager.LoadScene("QuestLog", LoadSceneMode.Additive);
            SceneManager.LoadScene("DialogueSystem", LoadSceneMode.Additive);

            _updatables = _buttons.Select(x => x.GetComponent<IUpdatableUI>()).ToArray();

            _cam = Camera.main;

            _cowLayer = LayerMask.NameToLayer("Cow");
        }

        private void Start()
        {
            foreach (var cow in PersistencyManager.Instance.SaveData.OwnedCows)
            {
                AddNewCow(cow);
            }
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Q))
            {
                Questlog.Instance.gameObject.SetActive(true);
            }
        }

        public void AddNewCow(string key)
        {
            var go = Instantiate(_cowPrefab, Vector2.zero, Quaternion.identity);
            var info = CowManager.Instance.GetCow(key);
            go.GetComponent<SpriteRenderer>().sprite = info.Sprite;

            var fcc = go.GetComponent<FarmCowController>();
            fcc.Info = info;
            Cows.Add(fcc);

            QuestEvents.Instance.BoughtACow();
        }

        public void SetMilkingAction()
        {
            ClickAction = ClickAction.Milk;
        }

        private IEnumerator Milk(FarmCowController cow)
        {
            PersistencyManager.Instance.SaveData.Energy -= 50;
            PersistencyManager.Instance.Save();
            foreach (var e in _updatables)
            {
                e.UpdateUI();
            }


            if (cow.Info.Conversation == null)
            {
                _preventButtonInteractions.SetActive(true);
                cow.IsBeingMilked = true;
                _milkingPreview.SetActive(true);
                _milkingCam.transform.position = new(cow.transform.position.x, cow.transform.position.y, _milkingCam.transform.position.z);

                yield return new WaitForSeconds(1f);

                _preventButtonInteractions.SetActive(false);
                cow.IsBeingMilked = false;
                _milkingPreview.SetActive(false);
            }
            else
            {
                DialogueManager.Instance.gameObject.SetActive(true);
                DialogueManager.Instance.StartConversation(cow.Info.Conversation);
            }

            if (cow.Info.NextCow != null)
            {
                PersistencyManager.Instance.SaveData.OwnedCows.Remove(cow.Info.Key);
                cow.Info = cow.Info.NextCow;
                PersistencyManager.Instance.SaveData.OwnedCows.Add(cow.Info.Key);
                PersistencyManager.Instance.Save();
            }

            QuestEvents.Instance.MilkedACow(cow.Info);
        }

        class CowElementComparer : IEqualityComparer<CowInfo>
        {
            public bool Equals(CowInfo x, CowInfo y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
                return x.Element == y.Element;
            }
            public int GetHashCode(CowInfo cow)
            {
                //Check whether the object is null
                if (ReferenceEquals(cow, null)) return 0;
                return cow.Element.GetHashCode();
            }
        }
        public void OnClick(InputAction.CallbackContext value)
        {
            if (value.performed && !_upgradeContainer.gameObject.activeInHierarchy)
            {
                _selectedCow = null;
                if (ClickAction != ClickAction.None)
                {
                    var hit = Physics2D.Raycast(_cam.ScreenToWorldPoint(Mouse.current.position.ReadValue()), Vector2.zero, float.MaxValue, 1 << _cowLayer);

                    if (hit.collider != null)
                    {
                        _selectedCow = hit.collider.gameObject.GetComponent<FarmCowController>();
                        if (ClickAction == ClickAction.Upgrade)
                        {
                            _upgradeContainer.gameObject.SetActive(true);
                            for (int i = 0; i < _upgradeContainer.childCount; i++)
                            {
                                Destroy(_upgradeContainer.GetChild(i).gameObject);
                            }

                            foreach (var element in CowManager.Instance.AllCows.Where(x => x.IsStartingCow && PersistencyManager.Instance.SaveData.Potions.ContainsKey(x.Element)).Distinct(new CowElementComparer()))
                            {
                                var curr = element;

                                var go = Instantiate(_upgradePrefab, _upgradeContainer);
                                go.GetComponentInChildren<TMP_Text>().text = $"{curr.Element} Potion";
                                go.GetComponent<Button>().onClick.AddListener(new(() =>
                                {
                                    PersistencyManager.Instance.SaveData.RemovePotion(curr.Element);
                                    PersistencyManager.Instance.SaveData.OwnedCows.Remove("NEUTRAL");
                                    PersistencyManager.Instance.SaveData.OwnedCows.Add(curr.Key);
                                    PersistencyManager.Instance.Save();
                                    _selectedCow.Info = curr;
                                    _upgradeContainer.gameObject.SetActive(false);
                                    ClickAction = ClickAction.None;

                                    QuestEvents.Instance.UpgradedACow(_selectedCow.Info);

                                    foreach (var e in _updatables)
                                    {
                                        e.UpdateUI();
                                    }
                                }));
                            }
                            var cancel = Instantiate(_upgradePrefab, _upgradeContainer);
                            cancel.GetComponentInChildren<TMP_Text>().text = "Cancel";
                            cancel.GetComponent<Button>().onClick.AddListener(new(() =>
                            {
                                _upgradeContainer.gameObject.SetActive(false);
                                ClickAction = ClickAction.None;
                            }));
                        }
                        else if (ClickAction == ClickAction.Milk)
                        {
                            StartCoroutine(Milk(_selectedCow));
                        }
                    }

                    foreach (var cow in Cows)
                    {
                        cow.Allow();
                    }
                    ClickAction = ClickAction.None;
                }
            }
        }

        public void Play()
        {
            SceneManager.LoadScene("Main");
        }
    }

    public enum ClickAction
    {
        None,
        Upgrade,
        Milk
    }
}
