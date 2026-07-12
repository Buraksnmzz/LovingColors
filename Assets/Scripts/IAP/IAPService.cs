using System;
using System.Collections.Generic;
using System.Linq;
using General;
using General.EventDispatcher;
using SavedData;
using UI.Settings;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;

namespace IAP
{
    public class IAPService : IIAPService
    {
        private readonly ISavedDataService _savedDataService;
        private readonly IEventDispatcherService _eventDispatcherService;

        private StoreController _storeController;

        public bool IsInitialized { get; private set; }

        private Action<bool> _purchaseCallback;
        private string _pendingPurchaseProductId;

        private readonly HashSet<string> _processedOrderIds = new();

        private const string NoAdsProductID = ProductIds.NoAdsOnly;
        private const string NoAdsPackProductID = ProductIds.NoAdsPack;

        public IAPService()
        {
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _ = InitializeAsync();
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                var options = new InitializationOptions().SetEnvironmentName("production");
                await UnityServices.InitializeAsync(options);
                _storeController = UnityIAPServices.StoreController();
                _storeController.OnPurchasePending += OnPurchasePending;
                _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                _storeController.OnPurchaseFailed += OnPurchaseFailed;
                await _storeController.Connect();
                _storeController.OnProductsFetched += OnProductsFetched;
                _storeController.OnProductsFetchFailed += OnProductsFetchFailed;
                _storeController.OnPurchasesFetched += OnPurchasesFetched;
                _storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                _storeController.OnStoreDisconnected += OnStoreDisconnected;

                var products = ProductIds.ProductTypeMap
                    .Select(kv => new ProductDefinition(kv.Key, kv.Value))
                    .ToList();

                _storeController.FetchProducts(products);
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAP] InitializeAsync failed: {e.Message}");
                IsInitialized = false;
                DispatchBannerVisibility();
            }
        }

        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"[IAP] Products fetched: {products.Count}");
            IsInitialized = true;
            _storeController.FetchPurchases();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"[IAP] Products fetch failed: {failure.FailureReason}");
            IsInitialized = false;
            DispatchBannerVisibility();
        }

        private void OnPurchasesFetched(Orders orders)
        {
            Debug.Log($"[IAP] Purchases fetched. Confirmed orders: {orders.ConfirmedOrders.Count}");

            bool ownsNoAds = orders.ConfirmedOrders.Any(o =>
                o.CartOrdered.Items().Any(item =>
                    item.Product.definition.id == NoAdsProductID ||
                    item.Product.definition.id == NoAdsPackProductID));

            SyncNoAdsEntitlement(ownsNoAds, source: "FetchPurchases");
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogWarning($"[IAP] Purchases fetch failed: {failure.FailureReason} | {failure.Message}");
            DispatchBannerVisibility();
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogWarning($"[IAP] Store disconnected: {failure.Message}");
            IsInitialized = false;
            DispatchBannerVisibility();
        }

        public void Purchase(string productId, Action<bool> onComplete)
        {
            _purchaseCallback = onComplete;

            if (!IsInitialized || _storeController == null)
            {
                Debug.LogError("[IAP] Purchase called before initialization.");
                FailPurchase();
                return;
            }

            var product = _storeController.GetProductById(productId);
            if (product == null || !product.availableToPurchase)
            {
                Debug.LogError($"[IAP] Product not available: {productId}");
                FailPurchase();
                return;
            }

            Debug.Log($"[IAP] Initiating purchase: {productId}");
            _pendingPurchaseProductId = productId;
            _storeController.PurchaseProduct(product);
        }

        private void OnPurchasePending(PendingOrder pendingOrder)
        {
            var orderId = pendingOrder.Info.TransactionID;
            var productId = pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition.id;

            Debug.Log($"[IAP] OnPurchasePending | orderId:{orderId} | product:{productId}");

            if (orderId != null && _processedOrderIds.Contains(orderId))
            {
                Debug.Log($"[IAP] Duplicate order skipped: {orderId}");
                _storeController.ConfirmPurchase(pendingOrder);
                return;
            }

            if (orderId != null)
                _processedOrderIds.Add(orderId);

            _storeController.ConfirmPurchase(pendingOrder);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case FailedOrder failedOrder:
                    OnPurchaseConfirmationFailed(failedOrder);
                    break;
                case ConfirmedOrder confirmedOrder:
                    OnPurchaseConfirmed(confirmedOrder);
                    break;
            }
        }

        private void OnPurchaseConfirmed(ConfirmedOrder confirmedOrder)
        {
            var product = confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product;
            var productId = product?.definition.id;
            var transactionId = confirmedOrder.Info.TransactionID;

            Debug.Log($"[IAP] Purchase confirmed: {productId} | transactionId:{transactionId}");

            bool isUserInitiated = _purchaseCallback != null && _pendingPurchaseProductId == productId;
            bool isNoAdsPurchase = productId == NoAdsProductID || productId == NoAdsPackProductID;

            if (isUserInitiated)
            {
                var settingsModel = _savedDataService.GetModel<SettingsModel>();
                bool alreadyOwned = isNoAdsPurchase && settingsModel.IsNoAds;

                if ((!isNoAdsPurchase || !alreadyOwned) && product != null)
                    YoogoLabManager.IAP(product);
            }

            if (productId == NoAdsProductID || productId == NoAdsPackProductID)
            {
                var settingsModel = _savedDataService.GetModel<SettingsModel>();
                settingsModel.IsNoAds = true;
                _savedDataService.SaveData(settingsModel);
            }

            if (isUserInitiated)
                _purchaseCallback.Invoke(true);

            ClearPurchaseState();
            DispatchBannerVisibility();
        }

        private void OnPurchaseConfirmationFailed(FailedOrder failedOrder)
        {
            var productId = failedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition.id;
            Debug.LogWarning($"[IAP] Purchase confirmation failed: {productId} | {failedOrder.FailureReason}");

            bool isUserInitiated = _purchaseCallback != null && _pendingPurchaseProductId == productId;
            if (isUserInitiated)
                FailPurchase();
        }

        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            var productId = failedOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition.id;
            var failureReason = failedOrder.FailureReason;
            Debug.LogWarning($"[IAP] Purchase failed: {productId} | {failureReason}");
            FailPurchase();
        }


        public void RestorePurchasesIOS()
        {
#if UNITY_IOS
            if (!IsInitialized || _storeController == null)
            {
                Debug.LogWarning("[IAP] RestorePurchases called before initialization.");
                return;
            }
            Debug.Log("[IAP] iOS RestorePurchases → RestoreTransactions");
            _storeController.RestoreTransactions((success, error) =>
            {
                Debug.Log($"[IAP] RestorePurchases completed. Success: {success} | Error: {error}");

                if (success)
                {
                    _storeController.FetchPurchases();
                    return;
                }

                DispatchBannerVisibility();
            });
#endif
        }


        private void SyncNoAdsEntitlement(bool ownsNoAds, string source)
        {
            var settingsModel = _savedDataService.GetModel<SettingsModel>();
            Debug.Log($"[IAP SYNC:{source}] before:{settingsModel.IsNoAds} → after:{ownsNoAds}");
            settingsModel.IsNoAds = ownsNoAds;
            _savedDataService.SaveData(settingsModel);
            DispatchBannerVisibility();
        }

        public string GetLocalizedPrice(string productId)
        {
            if (string.IsNullOrEmpty(productId) || !IsInitialized || _storeController == null)
                return string.Empty;

            var product = _storeController.GetProductById(productId);
            if (product?.metadata != null && !string.IsNullOrEmpty(product.metadata.localizedPriceString))
                return product.metadata.localizedPriceString;

            return string.Empty;
        }

        private void FailPurchase()
        {
            _purchaseCallback?.Invoke(false);
            ClearPurchaseState();
        }

        private void ClearPurchaseState()
        {
            _purchaseCallback = null;
            _pendingPurchaseProductId = null;
        }

        private void DispatchBannerVisibility()
        {
            var isNoAds = _savedDataService.GetModel<SettingsModel>().IsNoAds;
            _eventDispatcherService.Dispatch(new BannerVisibilityChangedSignal(!isNoAds));
        }
    }
}