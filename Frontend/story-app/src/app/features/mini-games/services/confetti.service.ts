import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ConfettiService {
  burst(canvas?: HTMLCanvasElement): void {
    const target = canvas ?? this.createCanvas();
    const ctx = target.getContext('2d')!;
    const particles = Array.from({ length: 80 }, () => ({
      x: Math.random() * target.width,
      y: Math.random() * target.height - target.height,
      r: Math.random() * 6 + 4,
      d: Math.random() * 80 + 20,
      color: `hsl(${Math.random() * 360},90%,60%)`,
      tilt: Math.random() * 10 - 10,
      tiltAngle: 0,
      tiltSpeed: Math.random() * 0.1 + 0.05
    }));

    let angle = 0;
    let frame = 0;
    const animate = () => {
      ctx.clearRect(0, 0, target.width, target.height);
      angle += 0.01;
      frame++;
      particles.forEach(p => {
        p.tiltAngle += p.tiltSpeed;
        p.y += (Math.cos(angle + p.d) + 2 + p.r / 2) * 1.5;
        p.x += Math.sin(angle);
        p.tilt = Math.sin(p.tiltAngle) * 12;
        ctx.beginPath();
        ctx.lineWidth = p.r;
        ctx.strokeStyle = p.color;
        ctx.moveTo(p.x + p.tilt + p.r / 4, p.y);
        ctx.lineTo(p.x + p.tilt, p.y + p.tilt + p.r / 4);
        ctx.stroke();
      });
      if (frame < 180) requestAnimationFrame(animate);
      else { ctx.clearRect(0, 0, target.width, target.height); if (!canvas) target.remove(); }
    };
    animate();
  }

  private createCanvas(): HTMLCanvasElement {
    const c = document.createElement('canvas');
    Object.assign(c.style, {
      position: 'fixed', top: '0', left: '0',
      width: '100vw', height: '100vh',
      pointerEvents: 'none', zIndex: '9999'
    });
    c.width = window.innerWidth;
    c.height = window.innerHeight;
    document.body.appendChild(c);
    return c;
  }
}
