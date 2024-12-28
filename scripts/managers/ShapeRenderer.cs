using System;
using System.Collections.Generic;
using Godot;

public partial class ShapeRenderer : Node2D
{
    public List<CircleData> circles;
    private const float maxCircleRadius = 17f;
    private const float speed = 10;

    public override void _Ready()
    {
        circles = new List<CircleData>();
    }

    public override void _Process(double delta)
    {
        var circlesToDelete = new List<CircleData>();
        for (int i = 0; i < circles.Count; i++)
        {
            var circle = circles[i];
            circle.radius += speed * (float)delta;
            circles[i] = circle;
            if (circle.radius >= maxCircleRadius)
            {
                circlesToDelete.Add(circle);
            }
        }
        foreach (var circle in circlesToDelete)
        {
            circles.Remove(circle);
        }
        if (circles.Count != 0)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (circles.Count > 0)
        {
            foreach (var circle in circles)
            {
                DrawCircle(circle.position, circle.radius, Color.Color8(255, 0, 0, 255));
            }
        }
    }
    public void addCircle(Vector2 pos)
    {
        circles.Add(new CircleData(pos, 1));
    }

    public struct CircleData
    {
        public Vector2 position;
        public float radius;

        public CircleData(Vector2 pos, float r)
        {
            position = pos;
            radius = r;
        }
    }
}