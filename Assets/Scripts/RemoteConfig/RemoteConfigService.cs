using System;
using System.Collections.Generic;
using Collectible;
using GameConfig;
using General;
using Newtonsoft.Json.Linq;
using SavedData;
using UnityEngine;

namespace RemoteConfig
{
    public class RemoteConfigService : IRemoteConfigService
    {
        private readonly ISavedDataService _savedDataService;
        private readonly RemoteConfigModel _remoteConfigModel;

        public RemoteConfigService()
        {
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
        }

        private void SetCollectibleModel()
        {
            if (_savedDataService.HasData<CollectibleModel>())
            {
                return;
            }
            var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
            collectibleModel.TotalCoins = _remoteConfigModel.StartingCoins;
            collectibleModel.TotalHints = _remoteConfigModel.StartingHints;
            _savedDataService.SaveData(collectibleModel);
        }

        public bool ApplyFromRemoteConfigJson(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson) || rawJson == "{}")
                return false;

            try
            {
                var root = JObject.Parse(rawJson);
                ApplyString(root, "rate_trigger_levels", v => _remoteConfigModel.RateTriggerLevels = v);
                ApplyInt(root, "starting_coins", v => _remoteConfigModel.StartingCoins = v);
                ApplyInt(root, "win_reward_coins", v => _remoteConfigModel.WinRewardCoins = v);
                ApplyInt(root, "win_reward_experience", v => _remoteConfigModel.WinRewardExperience = v);
                ApplyInt(root, "target_experience", v => _remoteConfigModel.TargetExperience = v);
                ApplyInt(root, "starting_hints", v => _remoteConfigModel.StartingHints = v);
                ApplyInt(root, "starting_pins", v => _remoteConfigModel.StartingPins = v);
                ApplyInt(root, "starting_super_pins", v => _remoteConfigModel.StartingSuperPins = v);
                ApplyInt(root, "hint_cost", v => _remoteConfigModel.HintCost = v);
                ApplyInt(root, "pin_cost", v => _remoteConfigModel.PinCost = v);
                ApplyInt(root, "super_pin_cost", v => _remoteConfigModel.SuperPinCost = v);
                ApplyInt(root, "extra_moves_cost", v => _remoteConfigModel.ExtraMovesCost = v);
                ApplyInt(root, "extra_moves_count", v => _remoteConfigModel.ExtraMovesCount = v);
                ApplyInt(root, "no_ads_pack_coin_reward", v => _remoteConfigModel.NoAdsPackCoinReward = v);
                ApplyInt(root, "shop_coin_video_reward", v => _remoteConfigModel.ShopCoinVideoReward = v);
                ApplyInt(root, "shop_coin_reward1", v => _remoteConfigModel.ShopCoinReward1 = v);
                ApplyInt(root, "shop_coin_reward2", v => _remoteConfigModel.ShopCoinReward2 = v);
                ApplyInt(root, "shop_coin_reward3", v => _remoteConfigModel.ShopCoinReward3 = v);
                ApplyInt(root, "shop_coin_reward4", v => _remoteConfigModel.ShopCoinReward4 = v);
                ApplyInt(root, "shop_coin_reward5", v => _remoteConfigModel.ShopCoinReward5 = v);
                ApplyInt(root, "hint_reward_with_coin", v => _remoteConfigModel.HintRewardWithCoin = v);
                ApplyInt(root, "pin_reward_with_coin", v => _remoteConfigModel.PinRewardWithCoin = v);
                ApplyInt(root, "super_pin_reward_with_coin", v => _remoteConfigModel.SuperPinRewardWithCoin = v);

                _savedDataService.SaveData(_remoteConfigModel);
                SetCollectibleModel();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[RemoteConfig] Failed to parse/apply RC: " + e.Message);
                return false;
            }
        }


        private void ApplyString(JObject root, string key, Action<string> setter)
        {
            if (!root.TryGetValue(key, out var token))
                return;

            var value = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
            if (string.IsNullOrWhiteSpace(value))
                return;

            setter(value);
        }

        private void ApplyInt(JObject root, string key, Action<int> setter)
        {
            if (!root.TryGetValue(key, out var token))
                return;

            if (!TryReadInt(token, out var value))
                return;

            setter(value);
        }

        private void ApplyIntArray(JObject root, string key, Action<int[]> setter)
        {
            if (!root.TryGetValue(key, out var token))
                return;

            var value = ReadIntArray(token);
            if (value.Length == 0)
                return;

            setter(value);
        }

        private bool TryReadInt(JToken token, out int value)
        {
            if (token.Type == JTokenType.Integer)
            {
                value = token.Value<int>();
                return true;
            }

            if (token.Type == JTokenType.Float)
            {
                value = (int)token.Value<float>();
                return true;
            }

            if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out var parsed))
            {
                value = parsed;
                return true;
            }

            value = 0;
            return false;
        }

        private int[] ReadIntArray(JToken token)
        {
            if (token.Type == JTokenType.Array)
            {
                var values = new List<int>();
                foreach (var item in token)
                {
                    if (TryReadInt(item, out var v))
                        values.Add(v);
                }

                return values.ToArray();
            }

            if (token.Type == JTokenType.String)
            {
                var s = token.Value<string>();
                if (string.IsNullOrWhiteSpace(s))
                    return Array.Empty<int>();

                var parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var values = new List<int>(parts.Length);
                for (var i = 0; i < parts.Length; i++)
                {
                    if (int.TryParse(parts[i].Trim(), out var parsed))
                        values.Add(parsed);
                }

                return values.ToArray();
            }

            if (TryReadInt(token, out var single))
                return new[] { single };

            return Array.Empty<int>();
        }
    }
}
