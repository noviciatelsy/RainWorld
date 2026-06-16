using System.Collections.Generic;
using UnityEngine;

public static class ToyCarRegistry
{
    private static readonly List<ToyCarController> activeCars = new List<ToyCarController>();

    public static int ActiveCount => activeCars.Count;

    public static void Register(ToyCarController car)
    {
        if (car == null || activeCars.Contains(car))
        {
            return;
        }

        activeCars.Add(car);
    }

    public static void Unregister(ToyCarController car)
    {
        if (car == null)
        {
            return;
        }

        activeCars.Remove(car);
    }

    public static bool TryFindClosest(
        Vector2 from,
        float queryRadius,
        out ToyCarController closest,
        out float closestDistSqr)
    {
        PruneInvalidCars();

        closest = null;
        closestDistSqr = float.MaxValue;
        float queryRadiusSqr = queryRadius * queryRadius;

        for (int i = 0; i < activeCars.Count; i++)
        {
            ToyCarController car = activeCars[i];
            if (car == null || !car.IsAttracting)
            {
                continue;
            }

            Vector2 carPosition = car.AttractionCenter;
            float distSqr = ((Vector2)carPosition - from).sqrMagnitude;

            if (distSqr > queryRadiusSqr)
            {
                continue;
            }

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = car;
            }
        }

        return closest != null;
    }

    public static bool HasActiveCar()
    {
        PruneInvalidCars();
        return activeCars.Count > 0;
    }

    private static void PruneInvalidCars()
    {
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            ToyCarController car = activeCars[i];
            if (car == null || !car.IsAttracting)
            {
                activeCars.RemoveAt(i);
            }
        }
    }
}
