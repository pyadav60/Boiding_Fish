using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Schooling : MonoBehaviour
{
    public GameObject FishPrefab;           
    public Material BubbleMaterial;        
    public GameObject BubblePrefab;         
    public Vector3 TankSize = new Vector3(10, 10, 20);  

    public int FishCount = 50;             
    public float FishSpeed = 3.5f;          
    public float NeighborDistance = 3f;     
    public float RotationSpeed = 5f;        
    public bool EnableTrails = true;        

    // Booleans to toggle forces
    public bool EnableCohesion = true;      // Flock Centering
    public bool EnableAlignment = true;     // Velocity Matching
    public bool EnableSeparation = true;    // Collision Avoidance
    public bool EnableWandering = true;    // Wandering
    public bool EnableWallAvoidance = true; // Wall Avoidance

    // Weights for balancing forces
    public float CohesionWeight = 1.0f;
    public float AlignmentWeight = 1.0f;
    public float SeparationWeight = 1.5f;
    public float WanderingWeight = 0.5f;
    public float WallAvoidanceWeight = 2.0f;

    // Wandering parameters
    public float WanderingStrength = 0.5f;  

    // Speed variables (Clamping)
    public float MinSpeed = 2f;             
    public float MaxSpeed = 5f;             
    public float AccelerationMultiplier = 2.0f; // Controls acceleration due to steering forces

    // Bubble trail parameters
    public int MaxBubblesPerTrail = 10;     
    public float BubbleFadeDuration = 2f;   
    public float BubbleSpawnInterval = 0.5f;

    // Wall avoidance parameters
    public float WallThreshold = 2.0f;      
    public float LookAheadDistance = 5.0f;  

    private List<Fish> fishes = new List<Fish>();          
    private Queue<GameObject> bubblePool = new Queue<GameObject>();  // Pool for reusing bubbles

    private int previousFishCount;

    void Start()
    {
        previousFishCount = FishCount;
        SpawnFish();
        DrawTankWireframe();
    }

    void Update()
    {
        if (FishCount != previousFishCount)
        {
            AdjustFishCount();
            previousFishCount = FishCount;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ScatterFish();
        }

        UpdateFishBehavior();
    }

    private void SpawnFish()
    {
        for (int i = 0; i < FishCount; i++)
        {
            AddFish();
        }
    }

    private void AddFish()
    {
        Vector3 randomPosition = GetRandomPositionInTank();
        GameObject fishObj = Instantiate(FishPrefab, randomPosition, Quaternion.identity, transform);
        Fish fish = new Fish(fishObj, this, BubbleSpawnInterval, FishSpeed);
        fishes.Add(fish);
    }

    private void RemoveFish()
    {
        if (fishes.Count == 0) return;

        Fish fishToRemove = fishes[fishes.Count - 1];
        Destroy(fishToRemove.Obj);
        fishes.RemoveAt(fishes.Count - 1);
    }

    private void AdjustFishCount()
    {
        if (FishCount > fishes.Count)
        {
            int fishToAdd = FishCount - fishes.Count;
            for (int i = 0; i < fishToAdd; i++)
            {
                AddFish();
            }
        }
        else if (FishCount < fishes.Count)
        {
            int fishToRemove = fishes.Count - FishCount;
            for (int i = 0; i < fishToRemove; i++)
            {
                RemoveFish();
            }
        }
    }

    private void UpdateFishBehavior()
    {
        foreach (Fish fish in fishes)
        {
            Vector3 cohesion = Vector3.zero;    // Flock Centering
            Vector3 separation = Vector3.zero;  // Collision Avoidance
            Vector3 alignment = Vector3.zero;   // Velocity Matching
            Vector3 wandering = Vector3.zero;   // Wandering
            Vector3 wallAvoidance = Vector3.zero; // Wall Avoidance
            int neighborCount = 0;

            // Calculate Cohesion, Separation, and Alignment
            foreach (Fish otherFish in fishes)
            {
                if (otherFish == fish) continue;

                Vector3 offset = otherFish.Position - fish.Position;
                float distance = offset.magnitude;

                if (distance < NeighborDistance)
                {
                    if (EnableCohesion)
                        cohesion += otherFish.Position;

                    if (EnableSeparation)
                        separation -= offset / (distance * distance);

                    if (EnableAlignment)
                        alignment += otherFish.Direction;

                    neighborCount++;
                }
            }

            if (neighborCount > 0)
            {
                if (EnableCohesion)
                    cohesion = ((cohesion / neighborCount) - fish.Position).normalized;

                if (EnableAlignment)
                    alignment = (alignment / neighborCount).normalized;
            }

            // Wandering
            if (EnableWandering)
            {
                wandering = fish.GetWanderingDirection() * WanderingStrength;
            }

            // Wall Avoidance
            if (EnableWallAvoidance)
            {
                wallAvoidance = ComputeWallAvoidance(fish) * WallAvoidanceWeight;
            }

            // Combine enabled forces
            Vector3 steeringForce = Vector3.zero;

            if (EnableCohesion)
                steeringForce += cohesion * CohesionWeight;

            if (EnableSeparation)
                steeringForce += separation * SeparationWeight;

            if (EnableAlignment)
                steeringForce += alignment * AlignmentWeight;

            if (EnableWandering)
                steeringForce += wandering * WanderingWeight;

            if (EnableWallAvoidance)
                steeringForce += wallAvoidance;

            // Normalize the steering force
            if (steeringForce != Vector3.zero)
            {
                steeringForce = steeringForce.normalized;
            }

            // Calculate acceleration
            float acceleration = steeringForce.magnitude * AccelerationMultiplier;

            // Update fish speed
            fish.Speed += acceleration * Time.deltaTime;
            fish.Speed = Mathf.Clamp(fish.Speed, MinSpeed, MaxSpeed);

            // Update fish direction
            if (steeringForce != Vector3.zero)
            {
                fish.Direction = Vector3.Slerp(fish.Direction, steeringForce, Time.deltaTime * RotationSpeed);
            }

            // Move the fish with variable speed
            fish.Position += fish.Direction * fish.Speed * Time.deltaTime;

            fish.Position = new Vector3(
                Mathf.Clamp(fish.Position.x, -TankSize.x / 2, TankSize.x / 2),
                Mathf.Clamp(fish.Position.y, -TankSize.y / 2, TankSize.y / 2),
                Mathf.Clamp(fish.Position.z, -TankSize.z / 2, TankSize.z / 2)
            );
            fish.Obj.transform.position = fish.Position;

            // Rotate fish to face movement direction
            if (fish.Direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(fish.Direction);
                fish.Obj.transform.rotation = Quaternion.Slerp(fish.Obj.transform.rotation, targetRotation, Time.deltaTime * RotationSpeed);
            }

            // Handle bubble trail
            if (EnableTrails)
                fish.UpdateTrail(BubblePrefab, BubbleMaterial, bubblePool, transform, MaxBubblesPerTrail, BubbleFadeDuration);
        }
    }

    private Vector3 ComputeWallAvoidance(Fish fish)
    {
        Vector3 avoidanceForce = Vector3.zero;

        // Predict future position
        Vector3 futurePosition = fish.Position + fish.Direction * LookAheadDistance;

        // Left Wall (-X)
        float leftDistance = futurePosition.x - (-TankSize.x / 2);
        if (leftDistance < WallThreshold)
        {
            float strength = (WallThreshold - leftDistance) / WallThreshold;
            avoidanceForce += Vector3.right * strength;
        }

        // Right Wall (+X)
        float rightDistance = (TankSize.x / 2) - futurePosition.x;
        if (rightDistance < WallThreshold)
        {
            float strength = (WallThreshold - rightDistance) / WallThreshold;
            avoidanceForce += Vector3.left * strength;
        }

        // Bottom Wall (-Y)
        float bottomDistance = futurePosition.y - (-TankSize.y / 2);
        if (bottomDistance < WallThreshold)
        {
            float strength = (WallThreshold - bottomDistance) / WallThreshold;
            avoidanceForce += Vector3.up * strength;
        }

        // Top Wall (+Y)
        float topDistance = (TankSize.y / 2) - futurePosition.y;
        if (topDistance < WallThreshold)
        {
            float strength = (WallThreshold - topDistance) / WallThreshold;
            avoidanceForce += Vector3.down * strength;
        }

        // Back Wall (-Z)
        float backDistance = futurePosition.z - (-TankSize.z / 2);
        if (backDistance < WallThreshold)
        {
            float strength = (WallThreshold - backDistance) / WallThreshold;
            avoidanceForce += Vector3.forward * strength;
        }

        // Front Wall (+Z)
        float frontDistance = (TankSize.z / 2) - futurePosition.z;
        if (frontDistance < WallThreshold)
        {
            float strength = (WallThreshold - frontDistance) / WallThreshold;
            avoidanceForce += Vector3.back * strength;
        }

        return avoidanceForce.normalized;
    }

    private void ScatterFish()
    {
        foreach (Fish fish in fishes)
        {
            fish.Position = GetRandomPositionInTank();
            fish.Direction = Random.onUnitSphere;  // Randomize initial direction
            fish.Speed = FishSpeed; // Reset speed to initial value
            fish.Obj.transform.position = fish.Position;
        }
    }

    private Vector3 GetRandomPositionInTank()
    {
        return new Vector3(
            Random.Range(-TankSize.x / 2, TankSize.x / 2),
            Random.Range(-TankSize.y / 2, TankSize.y / 2),
            Random.Range(-TankSize.z / 2, TankSize.z / 2)
        );
    }

    private void DrawTankWireframe()
    {
        // Define the corners of the rectangular prism
        Vector3[] corners = new Vector3[8]
        {
            new Vector3(-TankSize.x / 2, -TankSize.y / 2, -TankSize.z / 2), // Bottom-back-left
            new Vector3(TankSize.x / 2, -TankSize.y / 2, -TankSize.z / 2),  // Bottom-back-right
            new Vector3(-TankSize.x / 2, -TankSize.y / 2, TankSize.z / 2),  // Bottom-front-left
            new Vector3(TankSize.x / 2, -TankSize.y / 2, TankSize.z / 2),   // Bottom-front-right
            new Vector3(-TankSize.x / 2, TankSize.y / 2, -TankSize.z / 2),  // Top-back-left
            new Vector3(TankSize.x / 2, TankSize.y / 2, -TankSize.z / 2),   // Top-back-right
            new Vector3(-TankSize.x / 2, TankSize.y / 2, TankSize.z / 2),   // Top-front-left
            new Vector3(TankSize.x / 2, TankSize.y / 2, TankSize.z / 2)     // Top-front-right
        };

        // Define the lines connecting the corners
        int[,] edges = new int[12, 2]
        {
            {0, 1}, {1, 3}, {3, 2}, {2, 0}, // Bottom edges
            {4, 5}, {5, 7}, {7, 6}, {6, 4}, // Top edges
            {0, 4}, {1, 5}, {2, 6}, {3, 7}  // Vertical edges
        };

        // Create a parent object for the tank lines
        GameObject wireframeParent = new GameObject("TankWireframe");
        wireframeParent.transform.parent = transform;

        // Loop through and draw lines
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            GameObject line = new GameObject($"Line_{i}");
            line.transform.parent = wireframeParent.transform;

            LineRenderer lr = line.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);

            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.white;
            lr.endColor = Color.white;
        }

        wireframeParent.transform.position = transform.position;
    }

    // Fish Info
    private class Fish
    {
        public GameObject Obj { get; private set; }   
        public Vector3 Position { get; set; }        
        public Vector3 Direction { get; set; }        
        public float Speed { get; set; }              

        private float trailTimer;
        private Schooling schoolingInstance;

        private Queue<GameObject> trail = new Queue<GameObject>();

        // Wandering variables
        private float randomSeedX;
        private float randomSeedY;
        private float randomSeedZ;

        public Fish(GameObject obj, Schooling schooling, float spawnInterval, float initialSpeed)
        {
            Obj = obj;
            Position = obj.transform.position;
            Direction = obj.transform.forward;
            Speed = initialSpeed;
            schoolingInstance = schooling;
            trailTimer = spawnInterval;

            randomSeedX = Random.Range(0f, 100f);
            randomSeedY = Random.Range(0f, 100f);
            randomSeedZ = Random.Range(0f, 100f);
        }

        public Vector3 GetWanderingDirection()
        {
            float noiseScale = 1.0f; 
            float timeScale = 0.5f;  

            float noiseX = Mathf.PerlinNoise(randomSeedX, Time.time * timeScale) * 2 - 1;
            float noiseY = Mathf.PerlinNoise(randomSeedY, Time.time * timeScale) * 2 - 1;
            float noiseZ = Mathf.PerlinNoise(randomSeedZ, Time.time * timeScale) * 2 - 1;

            Vector3 wanderDir = new Vector3(noiseX, noiseY, noiseZ) * noiseScale;
            return wanderDir.normalized;
        }

        public void UpdateTrail(GameObject bubblePrefab, Material bubbleMaterial, Queue<GameObject> bubblePool, Transform parent, int maxBubbles, float fadeDuration)
        {
            trailTimer -= Time.deltaTime;
            if (trailTimer > 0) return;

            // Reset timer
            trailTimer = Random.Range(0.4f, 0.6f);

            // create a bubble
            GameObject bubble;
            if (bubblePool.Count > 0)
            {
                bubble = bubblePool.Dequeue();
                bubble.SetActive(true);
                // Reset material color with full opacity
                Renderer bubbleRenderer = bubble.GetComponent<Renderer>();
                bubbleRenderer.material = bubbleMaterial;
                bubbleRenderer.material.color = new Color(bubbleMaterial.color.r, bubbleMaterial.color.g, bubbleMaterial.color.b, 1f);
            }
            else
            {
                bubble = Instantiate(bubblePrefab, parent);
                bubble.GetComponent<Renderer>().material = bubbleMaterial;
            }

            bubble.transform.position = Position - Direction * 0.5f;

            // Random uniform scaling
            float randomScale = Random.Range(0.2f, 0.5f);
            bubble.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

            schoolingInstance.StartCoroutine(FadeBubble(bubble, fadeDuration, bubblePool));

            trail.Enqueue(bubble);
            if (trail.Count > maxBubbles)
            {
                trail.Dequeue(); 
            }
        }

        private IEnumerator FadeBubble(GameObject bubble, float duration, Queue<GameObject> bubblePool)
        {
            Renderer renderer = bubble.GetComponent<Renderer>();
            Color initialColor = renderer.material.color;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, timer / duration);
                renderer.material.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
                yield return null;
            }

            bubble.SetActive(false);
            bubblePool.Enqueue(bubble);
        }
    }
}
