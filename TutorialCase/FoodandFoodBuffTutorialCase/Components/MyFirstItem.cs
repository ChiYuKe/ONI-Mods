using UnityEngine;

namespace FoodandFoodBuffTutorialCase.Components
{
    public class MyFirstItem : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();
            Debug.Log("[MyFirstItem] spawned");
        }
    }
}