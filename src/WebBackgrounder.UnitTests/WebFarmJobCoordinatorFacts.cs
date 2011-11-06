﻿
using System;
using System.Collections.Generic;
using Moq;
using Xunit;
namespace WebBackgrounder.UnitTests
{
    public class WebFarmJobCoordinatorFacts
    {
        public class TheReserveWorkMethod
        {
            [Fact]
            public void ReturnsNullWhenActiveWorkersExist()
            {
                var repository = new Mock<IWorkItemRepository>();
                repository.Setup(r => r.GetLastWorkItem("jobname")).Returns(new Mock<IWorkItem>().Object);
                var coordinator = new WebFarmJobCoordinator(repository.Object);

                var unitOfWork = coordinator.ReserveWork("worker-id", "jobname");

                Assert.Null(unitOfWork);
            }

            [Fact]
            public void ReturnsNullWhenActiveWorkersExistWithinTransaction()
            {
                var complete = new Mock<IWorkItem>();
                complete.Setup(wi => wi.Completed).Returns(DateTime.UtcNow);
                var active = new Mock<IWorkItem>();
                active.Setup(wi => wi.Completed).Returns((DateTime?)null);

                var activeWorkerQueue = new Queue<IWorkItem>(new[] { complete.Object, active.Object });
                var repository = new Mock<IWorkItemRepository>();
                repository.Setup(r => r.GetLastWorkItem("jobname")).Returns(activeWorkerQueue.Dequeue);
                repository.Setup(r => r.GetLastWorkItem("jobname")).Returns(activeWorkerQueue.Dequeue);
                repository.Setup(r => r.RunInTransaction(It.IsAny<Action>())).Callback<Action>(a => a());
                repository.Setup(r => r.CreateWorkItem("worker-id", "jobname")).Throws(new InvalidOperationException());
                var coordinator = new WebFarmJobCoordinator(repository.Object);

                var unitOfWork = coordinator.ReserveWork("worker-id", "jobname");

                Assert.Null(unitOfWork);
            }

            [Fact]
            public void ReturnsUnitOfWorkWhenNoActiveWorkers()
            {
                var repository = new Mock<IWorkItemRepository>();
                repository.Setup(r => r.GetLastWorkItem("jobname")).Returns((IWorkItem)null);
                repository.Setup(r => r.RunInTransaction(It.IsAny<Action>())).Callback<Action>(a => a());
                repository.Setup(r => r.CreateWorkItem("worker-id", "jobname")).Returns(123);
                var coordinator = new WebFarmJobCoordinator(repository.Object);

                var unitOfWork = coordinator.ReserveWork("worker-id", "jobname");

                Assert.NotNull(unitOfWork);
            }
        }

        public class TheGetWorkMethod
        {
            [Fact]
            public void WithJobThatThrowsExceptionWhenCreatingTaskStillReturnsTask()
            {
                var job = new Mock<IJob>();
                job.Setup(j => j.Execute()).Throws(new InvalidOperationException("Test Exception"));
                var repository = new Mock<IWorkItemRepository>();
                repository.Setup(r => r.GetLastWorkItem("jobname")).Returns((IWorkItem)null);
                repository.Setup(r => r.RunInTransaction(It.IsAny<Action>())).Callback<Action>(a => a());
                repository.Setup(r => r.CreateWorkItem("worker-id", "jobname")).Returns(() => 123);
                var coordinator = new WebFarmJobCoordinator(repository.Object);

                var task = coordinator.GetWork(job.Object);

                Assert.NotNull(task);
            }

            [Fact]
            public void WithNullJobThrowsArgumentNullException()
            {
                var repository = new Mock<IWorkItemRepository>();
                var coordinator = new WebFarmJobCoordinator(repository.Object);

                var exception = Assert.Throws<ArgumentNullException>(() => coordinator.GetWork(null));

                Assert.Equal("job", exception.ParamName);
            }
        }

        public class TheCtor
        {
            [Fact]
            public void WithNullWorkItemRepositoryThrowsArgumentNullException()
            {
                var exception = Assert.Throws<ArgumentNullException>(() => new WebFarmJobCoordinator(null));

                Assert.Equal("workItemRepository", exception.ParamName);
            }
        }
    }
}
