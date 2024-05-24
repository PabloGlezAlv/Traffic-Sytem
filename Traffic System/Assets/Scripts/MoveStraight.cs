using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveStraight : MonoBehaviour
{
    // Vector que define la direcci�n y magnitud del movimiento
    public Vector3 movementVector;

    // Velocidad del movimiento
    public float speed = 1.0f;

    // Update se llama una vez por frame
    void Update()
    {
        // Mover el objeto en funci�n del vector de movimiento y la velocidad
        transform.Translate(movementVector * speed * Time.deltaTime);
    }

}
