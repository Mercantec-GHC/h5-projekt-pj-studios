# Docker Setup med Nginx Reverse Proxy

Dette setup giver dig mulighed for at hoste alle dine apps bag en enkelt Nginx reverse proxy.

## URL Struktur

- **`/`** → Redirect til `/flutter/`
- **`/flutter/`** → Flutter Web App
- **`/api/`** → Backend API

## Kommandoer

### Development
```bash
# Start alle services
docker-compose up -d

# Se logs
docker-compose logs -f

# Stop alle services
docker-compose down
```

### Production
```bash
# Start production setup
docker-compose -f compose.prod.yaml up -d

# Se logs
docker-compose -f compose.prod.yaml logs -f

# Stop production setup
docker-compose -f compose.prod.yaml down
```

## Cloudflare Tunnel Setup

1. **Installer cloudflared** på din server (Få kode af jeres underviser)
2. **Opret tunnel** i Cloudflare dashboard
3. **Konfigurer tunnel** til at pege på din server's port (I for udleveret en port af jeres underviser)
4. **Sæt custom domain** op (f.eks. `Gruppe1.mercantec.tech`) - Det opsætter jeres underviser


## Sikkerhed

### HTTPS med Cloudflare
- Cloudflare håndterer SSL/TLS automatisk
- Alle requests går gennem Cloudflare's proxy
- Din server behøver ikke SSL certifikater


## Troubleshooting

### Tjek service status:
```bash
docker-compose ps
```

### Se logs for specifik service:
```bash
docker-compose logs nginx
docker-compose logs backend
docker-compose logs flutterweb
```

### Test endpoints lokalt:
```bash
# Health check
curl http://localhost/health

# API test
curl http://localhost/api/weatherforecast

# Flutter app
curl http://localhost/flutter/

```

### Genstart specifik service:
```bash
docker-compose restart nginx
```

## Performance

### Nginx Caching
Nginx konfigurationen inkluderer:
- Static file caching
- Gzip compression
- Connection pooling

### Docker Optimizations
- Alpine Linux images for mindre størrelse
- Multi-stage builds i Dockerfiles
- Shared networks for bedre performance

## Monitoring

### Health Checks
Alle services har health check endpoints:
- Nginx: `/health`
- Backend: `/api/health` (hvis implementeret)

### Logs
- Nginx access logs
- Application logs per service
- Docker container logs 