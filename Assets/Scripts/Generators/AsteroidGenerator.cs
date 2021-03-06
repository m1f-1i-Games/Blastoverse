﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidGenerator : MonoBehaviour
{
    public ObjectPooler asteroidPool;

    [Header("Asteroid Properties")]
    public int minimumVertices;
    public int maximumVertices;
    public float semiMajorAxis;
    public float semiMinorAxis;
    public float thetaDeviation;

    [Header("Auto Size")]
    public bool autoSize;
    public float asteroidSize;

    [Header("Asteroid Splitting")]
    public float areaThreshold;
    public float momentumFactor;
    public float splitForce;
    public float splitRotation;

    [Header("Difficulty")]
    public float spawnRate;
    float currentRate;

    [Header("Sound Effects")]
    public Sound splitSFX;
    public Sound filteredSplitSFX;
    public Sound destructionSFX;

    public bool isEnabled;

    private void Awake()
    {
        GameManager.Instance.OnGameReady += OnGameReady;
    }

    private void OnGameReady()
    {
        isEnabled = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < asteroidPool.objects.Count; i++) 
        {
            asteroidPool.objects[i].GetComponent<Asteroid>().meshID = i;
        }

        if (autoSize)
        {
            semiMajorAxis = maximumVertices / 10f;
            semiMinorAxis = minimumVertices / 10f;
        }
        semiMajorAxis *= asteroidSize;
        semiMinorAxis *= asteroidSize;

        currentRate = spawnRate;
    }

    void Update()
    {
        if (isEnabled)
        {
            currentRate -= Time.deltaTime;
            if (currentRate < 0)
            {
                GenerateNewAsteroid();
            }
        }
    }

    void GenerateNewAsteroid() 
    {
        Asteroid asteroid = asteroidPool.GetPooledObject().GetComponent<Asteroid>();

        MeshData data = CyclicPolygonGenerator.GeneratePolygon(AsteroidGenerationRNG.Instance.Next(minimumVertices, maximumVertices), semiMajorAxis, semiMinorAxis, thetaDeviation);

        asteroid.SetMesh(data);
        asteroid.gameObject.SetActive(true);

        asteroid.InitialMove();

        asteroid.parentGenerator = this;

        currentRate = spawnRate;
    }

    public void GenerateAsteroidSlices(MeshData leftData, MeshData rightData, Vector2 leftCentroid, Vector2 rightCentroid, Vector3 preVelocity, Vector3 slicerVelocity)
    {
        Asteroid leftAsteroid, rightAsteroid;

        Vector3 preVelLeft = Quaternion.Euler(0, 0, 90) * slicerVelocity * splitForce;
        Vector3 preVelRight = Quaternion.Euler(0, 0, -90) * slicerVelocity * splitForce;

        bool spawnedLeft = false, spawnedRight = false;

        if (leftData.GetArea() > areaThreshold)
        {
            leftAsteroid = asteroidPool.ActivateObject().GetComponent<Asteroid>();
            leftAsteroid.parentGenerator = this;
            leftAsteroid.SetMesh(leftData);
            leftAsteroid.transform.position = leftCentroid;
            leftAsteroid.transform.rotation = Quaternion.identity;
            leftAsteroid.Move((preVelocity + slicerVelocity + preVelLeft) * momentumFactor, AsteroidGenerationRNG.Instance.NextFloat(-1, 1) * splitRotation);
            spawnedLeft = true;
        }

        if (rightData.GetArea() > areaThreshold)
        {
            rightAsteroid = asteroidPool.ActivateObject().GetComponent<Asteroid>();
            rightAsteroid.parentGenerator = this;
            rightAsteroid.SetMesh(rightData);
            rightAsteroid.transform.position = rightCentroid;
            rightAsteroid.transform.rotation = Quaternion.identity;
            rightAsteroid.Move((preVelocity + slicerVelocity + preVelRight) * momentumFactor, AsteroidGenerationRNG.Instance.NextFloat(-1, 1) * splitRotation);
            spawnedRight = true;
        }

        if (spawnedLeft && spawnedRight)
            AudioManager.Instance.PlaySoundAtPosition(splitSFX, transform.position);
        else if (spawnedLeft || spawnedRight)
            AudioManager.Instance.PlaySoundAtPosition(filteredSplitSFX, transform.position);
        else
            AudioManager.Instance.PlaySoundAtPosition(destructionSFX, transform.position);

    }

}
