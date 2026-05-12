# RabbitMQ Setup Instructions for EduLearn

## Prerequisites
1. Install RabbitMQ Server on your machine
2. Download and install from: https://www.rabbitmq.com/download.html

## Installation Steps

### Windows
1. Download the latest RabbitMQ installer
2. Run the installer as Administrator
3. RabbitMQ will be installed as a Windows service

### Verify Installation
1. Open Command Prompt as Administrator
2. Run: `rabbitmqctl status`
3. You should see RabbitMQ server status

### Management UI (Optional but Recommended)
1. Enable the management plugin:
   ```
   rabbitmq-plugins enable rabbitmq_management
   ```
2. Restart RabbitMQ service
3. Access management UI at: http://localhost:15672
4. Default credentials: guest/guest

## Configuration
The RabbitMQ configuration is already set up in your services' `appsettings.json`:

```json
"RabbitMQ": {
  "HostName": "localhost",
  "Port": 5672,
  "UserName": "guest",
  "Password": "guest",
  "VirtualHost": "/"
}
```

## Testing the Integration

### 1. Start RabbitMQ Server
Make sure RabbitMQ service is running:
```
rabbitmq-service start
```

### 2. Run Your Services
Start each microservice:
- CourseService
- EnrollmentService  
- PaymentService

### 3. Test Message Flow
When you perform actions like:
- Creating a course enrollment
- Updating a course
- Processing a payment

The services will publish messages to RabbitMQ exchanges:
- `course.exchange` - Course-related events
- `enrollment.exchange` - Enrollment-related events
- `payment.exchange` - Payment-related events
- `notification.exchange` - General notifications

### 4. Monitor Messages
Use the RabbitMQ Management UI to monitor:
- Exchanges
- Queues
- Message flow
- Consumer connections

## Common Issues & Solutions

### Connection Refused
- Make sure RabbitMQ service is running
- Check firewall settings for ports 5672 and 15672
- Verify connection settings in appsettings.json

### Authentication Failed
- Verify username/password (default: guest/guest)
- Check if user has permissions for the virtual host

### Messages Not Consumed
- Verify consumers are registered in Program.cs
- Check service logs for consumer errors
- Ensure exchanges and queues are properly bound

## Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   CourseService │    │ EnrollmentService│   │  PaymentService │
│                 │    │                 │   │                 │
│  Publisher      │    │ Publisher       │   │ Publisher       │
│  Consumer       │    │ Consumer        │   │ Publisher       │
└─────────┬───────┘    └─────────┬───────┘   └─────────┬───────┘
          │                      │                     │
          └──────────────────────┼─────────────────────┘
                                 │
                    ┌─────────────▼─────────────┐
                    │       RabbitMQ            │
                    │  ┌─────────────────────┐  │
                    │  │ course.exchange     │  │
                    │  │ enrollment.exchange  │  │
                    │  │ payment.exchange     │  │
                    │  │ notification.exchange│  │
                    │  └─────────────────────┘  │
                    └───────────────────────────┘
```

## Next Steps
1. Install RabbitMQ server
2. Start the RabbitMQ service
3. Run your microservices
4. Test the integration by creating enrollments and payments
5. Monitor message flow using the management UI
