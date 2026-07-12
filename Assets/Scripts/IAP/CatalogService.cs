using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace IAP
{
    public class CatalogProduct
    {
        public string Id;
        public int Coins;
    }

    public static class CatalogService
    {
        private static Dictionary<string, CatalogProduct> _cache = new();

        public static void LoadCatalog()
        {
            _cache.Clear();

            var catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog == null) return;

            foreach (var item in catalog.allValidProducts)
            {
                var cp = new CatalogProduct { Id = item.id };

                if (item.Payouts != null)
                {
                    foreach (var p in item.Payouts)
                    {
                        if (!string.IsNullOrEmpty(p.subtype))
                        {
                            var key = p.subtype.ToLower();
                            var qty = (int)p.quantity;
                            if (key.Contains("coin")) cp.Coins += qty;
                        }
                    }
                }

                _cache[item.id] = cp;
            }
        }

        public static CatalogProduct GetProduct(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_cache.TryGetValue(id, out var p)) return p;
            return null;
        }
    }
}