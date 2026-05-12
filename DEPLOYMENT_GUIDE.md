# EduLearn Backend Deployment Guide - Render

## Overview
This guide will help you deploy your EduLearn microservices backend to Render.com.

## Prerequisites
- GitHub repository: https://github.com/GavendraPratap727/EduLearn_Backend
- Render.com account (Free tier supports up to 7 services)
- PostgreSQL database (Render provides free PostgreSQL)

## Services to Deploy
Your backend consists of 8 microservices:
1. **AuthService** (Port 5001) - User authentication & JWT tokens
2. **CourseService** (Port 5002) - Course management
3. **EnrollmentService** (Port 5003) - Student enrollments
4. **LessonService** (Port 5004) - Lesson content
5. **PaymentService** (Port 5005) - Payment processing
6. **ProgressService** (Port 5006) - Student progress tracking
7. **QuizService** (Port 5007) - Quiz management
8. **ReviewService** (Port 5008) - Course reviews

## Step-by-Step Deployment

### Step 1: Push Deployment Files to GitHub
First, push these new deployment files to your GitHub repository:

```bash
git add Dockerfile start-services.sh render.yaml .dockerignore
git commit -m "Add Render deployment configuration"
git push origin main
```

### Step 2: Set Up Render Account
1. Go to [Render.com](https://render.com)
2. Sign up/login with GitHub
3. Verify your email if required

### Step 3: Create PostgreSQL Database
1. In Render dashboard, click **"New +"**
2. Select **"PostgreSQL"**
3. Configure:
   - Name: `EduLearn-DB`
   - Database Name: `edulearn`
   - User: `postgres`
   - Choose Free tier
4. Click **"Create Database"**
5. Wait for database to be ready (2-3 minutes)
6. Save the connection details (external URL)

### Step 4: Deploy Individual Services

#### Option A: Using render.yaml (Recommended)
1. In Render dashboard, click **"New +"**
2. Select **"Web Service"**
3. Connect your GitHub repository
4. Render will detect the `render.yaml` file
5. Review and create all services

#### Option B: Manual Setup (If render.yaml doesn't work)
For each service, repeat these steps:

**AuthService:**
1. Click **"New +"** → **"Web Service"**
2. Connect GitHub repo
3. Configure:
   - Name: `EduLearn-AuthService`
   - Environment: `Docker`
   - Branch: `main`
   - Root Directory: `EduLearn/src/Services/AuthService`
   - Plan: `Free`
4. Environment Variables:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   Jwt__SecretKey=your-super-secret-jwt-key-here
   Jwt__Issuer=EduLearn
   Jwt__Audience=EduLearnUsers
   DefaultConnection=your-postgres-connection-string
   ```
5. Click **"Create Web Service"**

**Repeat for other 7 services** with appropriate names and ports.

### Step 5: Configure Environment Variables
For each service, set these environment variables:

**Common for all services:**
```
ASPNETCORE_ENVIRONMENT=Production
DefaultConnection=postgresql://postgres:password@your-db-host:5432/edulearn
```

**AuthService specific:**
```
Jwt__SecretKey=your-super-secret-jwt-key-here
Jwt__Issuer=EduLearn
Jwt__Audience=EduLearnUsers
```

**PaymentService specific:**
```
Razorpay__KeyId=your-razorpay-key-id
Razorpay__KeySecret=your-razorpay-key-secret
```

### Step 6: Update Frontend Configuration
Update your Angular frontend to use the new Render URLs:

```typescript
// In your environment files
export const environment = {
  production: true,
  apiUrl: 'https://edulearn-authservice.onrender.com',
  courseUrl: 'https://edulearn-courseservice.onrender.com',
  enrollmentUrl: 'https://edulearn-enrollmentservice.onrender.com',
  // ... other service URLs
};
```

### Step 7: Test the Deployment
1. Wait for all services to finish building (5-10 minutes each)
2. Check the health endpoints:
   - `https://edulearn-authservice.onrender.com/api/auth/health`
   - `https://edulearn-courseservice.onrender.com/api/courses/health`
   - etc.
3. Test API endpoints through Swagger UI
4. Connect your frontend and test full integration

## Important Notes

### Database Setup
Each service needs its own database schema. You'll need to:
1. Create separate databases for each service
2. Run migrations for each service
3. Or use a single database with different schemas

### CORS Configuration
Update the CORS origins in each service's Program.cs:
```csharp
.WithOrigins(
    "http://localhost:4200",
    "https://your-frontend.onrender.com"
)
```

### Free Tier Limitations
- Render free tier: 750 hours/month
- PostgreSQL free tier: 90 days, then sleeps
- Services sleep after 15 minutes of inactivity
- Cold starts take 30-60 seconds

### Monitoring
- Check Render logs for any errors
- Monitor service health through health endpoints
- Set up alerts for critical services

## Troubleshooting

### Common Issues:
1. **Build Failures**: Check Dockerfile paths and dependencies
2. **Database Connection**: Verify connection string format
3. **CORS Errors**: Update allowed origins
4. **Port Conflicts**: Ensure each service uses different ports
5. **Environment Variables**: Check all required variables are set

### Getting Help:
- Check Render documentation: https://render.com/docs
- Review service logs in Render dashboard
- Test locally with Docker first

## Next Steps
1. Deploy frontend to Render
2. Set up custom domains
3. Configure SSL certificates
4. Set up monitoring and logging
5. Add CI/CD pipeline

Your EduLearn backend should now be running on Render! 🚀
