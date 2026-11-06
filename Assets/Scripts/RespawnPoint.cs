using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    private void Update()
    {
        transform.eulerAngles += new Vector3(0.0f, 0.0f, 45.0f * Time.deltaTime);
    }
}
