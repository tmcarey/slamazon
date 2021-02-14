using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Package : MonoBehaviour
{
    private GameObject packageIcon;
    private GameObject destinationIcon;

    [SerializeField] private float iconMargin;
    [SerializeField] private float minVelocity = 0.1f;
    [SerializeField] private GameObject deliveredEffect;
    [SerializeField] private LayerMask groundLayer;
    private GameObject m_BoxIcon;
    private Rigidbody rbody;
    private bool waitingForRest;
    private bool waitingForContact;
    
    
    private OrderManager.Order ord;


    public void SetOrder(OrderManager.Order col)
    {
        destination = col.destination.destinationPoint.position;
        mainMesh.material.color = col.color;
        ord = col;
    }

    public void Start()
    {
        rbody = GetComponent<Rigidbody>();
        iconCanvas = Global.Instance.iconCanvas;

        packageIcon = gameObject.transform.Find("PackageIcon").gameObject;
        destinationIcon = gameObject.transform.Find("DestinationIcon").gameObject;
        packageIcon.SetActive(false);
        destinationIcon.SetActive(false);
        //packageIcon.transform.SetParent(iconCanvas.transform, false);
        destinationIcon.transform.SetParent(iconCanvas.transform, false);

        Color color = UnityEngine.Random.ColorHSV(0f, 1f, 0.8f, 0.8f, 0.8f, 0.8f);
        packageIcon.GetComponent<Image>().color = color;
        destinationIcon.GetComponent<Image>().color = color;

    }

    public void UpdateIcon(Vector3 position, GameObject obj)
    {
        var screenPoint = Camera.main.WorldToScreenPoint(position);
        var canvasTransform = iconCanvas.transform as RectTransform;

        var w = canvasTransform.rect.width;
        var h = canvasTransform.rect.height;

        if (screenPoint.z < 0)
        {
            screenPoint.x = Mathf.Sign(screenPoint.x) * Mathf.NegativeInfinity;
        }

        obj.transform.position = new Vector3(
            Mathf.Clamp(screenPoint.x, w * iconMargin, w * (1f - iconMargin)),
            h * 0.9f);
    }

    public void Update()
    {
        UpdateIcon(this.transform.position, packageIcon);
        UpdateIcon(this.destination, destinationIcon);
    }

    public void OnDestroy()
    {
        Destroy(packageIcon);
        Destroy(destinationIcon);
    }

    [SerializeField] public Vector3 anchorPoint;


    public void OnPickedUp()
    {
        destinationIcon.SetActive(true);
        waitingForContact = false;

        waitingForRest = false;
        if (pl)
        {
            pl.OnPackagePickedUp();
            pl = null;
        }
    }

    public void OnRelease()
    {
        destinationIcon.SetActive(false);
        //waitingForRest = true;
        waitingForContact = true;
        //waitingForRest = true;
    }

    public void OnRest()
    {
        float distanceToDest = Vector3.Distance(transform.position, destination);
        Debug.Log(distanceToDest);
        if (distanceToDest > 10)
        {
            return;
        }
        Destroy(m_BoxIcon);
        ord.om.CompleteOrder(ord);
        Debug.Log("distanceToDest: " + distanceToDest);
        Global.Instance.AddScore((10 - distanceToDest) / (10F/10F));
        Global.Instance.AddPackage();
        Destroy(GameObject.Instantiate(deliveredEffect, transform.position, Quaternion.identity), 2);
        Destroy(gameObject);
    }

    private IEnumerator WaitForMotion(float t)
    {
        float _t = 0;
        while (_t < t)
        {
            _t += Time.fixedDeltaTime;
            if (rbody.velocity.magnitude > minVelocity)
            {
                waitingForRest = true;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        if (!waitingForRest)
        {
            OnRest();
        }
        
    }

    private void FixedUpdate()
    {
        if (waitingForRest)
        {
            if (rbody.velocity.magnitude < minVelocity)
            {
                waitingForRest = false;
                StartCoroutine(WaitForMotion(0.5f));
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (waitingForContact && (other.collider.gameObject.layer== 9))
        {
            OnRest();
        }
    }

    public PickupLocation pl;
    public Vector3 destination;
    public MeshRenderer mainMesh;
    public GameObject iconCanvas;

}
