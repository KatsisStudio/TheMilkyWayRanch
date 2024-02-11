﻿using UnityEngine;

namespace CowMilking.Character
{
    public abstract class ACharacter<T> : MonoBehaviour, ICharacter
        where T : ScriptableObject
    {

        protected T _info;
        public ScriptableObject Info
        {
            set
            {
                _info = (T)value;
                Init();
            }
        }

        public int ID => GetInstanceID();

        private int _health;

        protected abstract int BaseHealth { get; }

        protected void Init()
        {
            _health = BaseHealth;
        }

        public void TakeDamage()
        {
            _health--;
            if (_health <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
