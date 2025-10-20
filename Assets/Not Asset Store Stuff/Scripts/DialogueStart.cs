using cherrydev;
using Unity.VisualScripting;
using UnityEngine;

public class DialogueStart : MonoBehaviour
{
    [SerializeField] private DialogBehaviour DialogBehaviour;
    [SerializeField] private DialogNodeGraph DialogGraph;

    private void Start()
    {
        DialogBehaviour.StartDialog(DialogGraph);
    }
}
