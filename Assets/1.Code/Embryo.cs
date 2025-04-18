using UnityEngine;

public class Embryo : MonoBehaviour
{

  
    public float rotationSpeed = 0.5f; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
        
    }

    void FixedUpdate()
    {

    }
}
