using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConectionData : MonoBehaviour
{
    private int dependantCars = 0;

    // M�todo p�blico para obtener el valor de la variable privada
    public int GetDependantCars()
    {
        return dependantCars;
    }

    // M�todo p�blico para establecer el valor de la variable privada
    public void AddCar(int value)
    {
        dependantCars += value;
    }
}
