using System;
using System.Collections.Generic;
using Godot;

public partial class ShapeRenderer : Node2D
{
    public Dictionary<Projectile, CircleData> circles;
    private const float maxCircleRadius = 17f;
    private const float speed = 10;
    private EnemyBoss boss;
    public override void _Ready()
    {
        circles = new Dictionary<Projectile, CircleData>();
    }

    public override void _Process(double delta)
    {
        foreach (var c in circles)
        {
            var circle = c.Value;
            circle.radius += speed * (float)delta;
            circles[c.Key] = circle;
            c.Key.fallTime = circle.radius / maxCircleRadius;
            if (circle.radius >= maxCircleRadius)
            {
                circles.Remove(c.Key);
            }
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (circles.Count > 0)
        {
            foreach (var circle in circles)
            {
                DrawCircle(circle.Value.position, circle.Value.radius, Color.Color8(200, 0, 0, 125));
            }
        }
    }
    public void addCircle(Vector2 pos, EnemyBoss e)
    {
        if (boss == null)
        {
            boss = e;
        }
        var circle = new CircleData(pos, 1);
        var pro = PoolEngine.pool.pullFromPool<Projectile>();
        pro.init(circle.position + Vector2.Up * 500, Vector2.Down, Vector2.Down.Angle(), boss, 0, boss.data.angularSpeed, boss.data.isProjectile, "Bamboo", 0, 0);
        pro.Scale = Vector2.One * 2.75f;
        pro.ZIndex = 2;
        pro.Monitoring = false;
        pro.rayCast.Enabled = false;
        pro.specialAttack = true;
        circles.Add(pro, circle);
        QueueRedraw();
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