using UnityEngine;

public class Activator : MonoBehaviour
{
    public void Active(bool value) => gameObject.SetActive(value);

    public void ActiveReverse(bool value) => gameObject.SetActive(!value);
}
