using UnityEngine;
using System;
using System.Collections.Generic;

namespace BossSystem.Boss.FireBoss
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHP = 200f;
        private float currentHP;

        private void Awake() => currentHP = maxHP;

        public void TakeDamage(float amount)
        {
            currentHP -= amount;
            Debug.Log($"[Player] HP: {currentHP:F1}/{maxHP} (-{amount:F1})");
            if (currentHP <= 0f) Debug.Log("[Player] 사망");
        }
    }
}
