using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO
//  add coroutine to count time before enabling collisions again * DONE
//  prolem when GrabOrigin is the grabber :-( * DONE :-)

[RequireComponent(typeof(CapsuleCollider))]
public class FruitController : MonoBehaviour
{
     private Rigidbody rb;
     private Transform grabber;
     private bool isGrabbed;
     private CapsuleCollider triggerCapsule;

     private void OnTriggerEnter(Collider other)
     {
         if (other.CompareTag("Player") && other.GetComponent<SnakeController>() != null) {
             Debug.Log("Finding grabber...");
             grabber = other.GetComponent<SnakeController>().GrabOrigin.transform;
             //grabber = other.transform;
             Debug.Log(grabber);
             isGrabbed = true;
         }
     }

     private void Start() {
         triggerCapsule = GetComponents<CapsuleCollider>()[0];
         triggerCapsule.enabled = true;
         rb = GetComponent<Rigidbody>();
         rb.isKinematic = true;
         isGrabbed = false;
     }


     private void Update() {
         if (isGrabbed) {
             rb.isKinematic = true;
             transform.position = grabber.position;
             triggerCapsule.enabled = false;
         } else
         {
            //triggerCapsule.enabled = true;
            rb.isKinematic = false;
         } 
     }

     IEnumerator WaitEnableCollider() {
         yield return new WaitForSeconds(2);
         triggerCapsule.enabled = true;
     }

     public void Release() {
         isGrabbed = false;
         StartCoroutine(WaitEnableCollider());
         Debug.Log("Release");
     }
}
