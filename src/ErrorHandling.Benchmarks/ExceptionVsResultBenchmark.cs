using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ErrorHandling.Domain.Exceptions;
using ErrorHandling.Domain.Results;
using ErrorHandling.Domain.ValueObjects;

namespace ErrorHandling.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CsvExporter]
[HtmlExporter]
public class ExceptionVsResultBenchmark
{
    private const int Iterations = 1000;

    // Test data
    private readonly string validEmail = "test@example.com";
    private readonly string invalidEmail = "invalid-email";
    private readonly decimal validAmount = 100m;

    // Benchmark 1: Success path comparison
    [Benchmark]
    public void Exception_SuccessPath()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var email = Email.Create(validEmail);
            var money = Money.Create(validAmount);
        }
    }

    [Benchmark]
    public void Result_SuccessPath()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var email = Email.TryCreate(validEmail);
            var money = Money.TryCreate(validAmount);
        }
    }

    // Benchmark 2: Failure path comparison (shallow stack)
    [Benchmark]
    public int Exception_FailurePath_Shallow()
    {
        int caught = 0;
        for (int i = 0; i < Iterations; i++)
        {
            try
            {
                var email = Email.Create(invalidEmail);
            }
            catch (ValidationException)
            {
                caught++;
            }
        }
        return caught;
    }

    [Benchmark]
    public int Result_FailurePath_Shallow()
    {
        int failures = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = Email.TryCreate(invalidEmail);
            if (result.IsFailure)
                failures++;
        }
        return failures;
    }

    // Benchmark 3: Deep call stack with exceptions
    [Benchmark]
    public int Exception_DeepCallStack()
    {
        int processed = 0;
        for (int i = 0; i < Iterations; i++)
        {
            try
            {
                processed += ProcessOrderWithExceptions(i % 2 == 0);
            }
            catch (Exception)
            {
                // Handle error
            }
        }
        return processed;
    }

    [Benchmark]
    public int Result_DeepCallStack()
    {
        int processed = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = ProcessOrderWithResults(i % 2 == 0);
            if (result.IsSuccess)
                processed += result.Value;
        }
        return processed;
    }

    // Benchmark 4: Multiple validation errors
    [Benchmark]
    public int Exception_MultipleValidations()
    {
        int valid = 0;
        for (int i = 0; i < Iterations; i++)
        {
            try
            {
                ValidateWithExceptions($"user{i}", i.ToString(), i);
                valid++;
            }
            catch
            {
                // Validation failed
            }
        }
        return valid;
    }

    [Benchmark]
    public int Result_MultipleValidations()
    {
        int valid = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = ValidateWithResults($"user{i}", i.ToString(), i);
            if (result.IsSuccess)
                valid++;
        }
        return valid;
    }

    // Helper methods for deep call stack
    private int ProcessOrderWithExceptions(bool shouldSucceed)
    {
        ValidateOrderExceptions(shouldSucceed);
        CheckInventoryExceptions(shouldSucceed);
        ProcessPaymentExceptions(shouldSucceed);
        return 1;
    }

    private void ValidateOrderExceptions(bool shouldSucceed)
    {
        if (!shouldSucceed)
            throw new ValidationException("Order validation failed");
        CallDepth1Exceptions(shouldSucceed);
    }

    private void CallDepth1Exceptions(bool shouldSucceed)
    {
        CallDepth2Exceptions(shouldSucceed);
    }

    private void CallDepth2Exceptions(bool shouldSucceed)
    {
        CallDepth3Exceptions(shouldSucceed);
    }

    private void CallDepth3Exceptions(bool shouldSucceed)
    {
        CallDepth4Exceptions(shouldSucceed);
    }

    private void CallDepth4Exceptions(bool shouldSucceed)
    {
        CallDepth5Exceptions(shouldSucceed);
    }

    private void CallDepth5Exceptions(bool shouldSucceed)
    {
        if (!shouldSucceed)
            throw new BusinessRuleException("DEEP_ERROR", "Error at depth 5");
    }

    private void CheckInventoryExceptions(bool shouldSucceed)
    {
        if (!shouldSucceed)
            throw new BusinessRuleException("INSUFFICIENT_STOCK", "Not enough stock");
    }

    private void ProcessPaymentExceptions(bool shouldSucceed)
    {
        if (!shouldSucceed)
            throw new BusinessRuleException("PAYMENT_FAILED", "Payment processing failed");
    }

    private Result<int> ProcessOrderWithResults(bool shouldSucceed)
    {
        var validationResult = ValidateOrderResults(shouldSucceed);
        if (validationResult.IsFailure)
            return Result<int>.Failure(validationResult.Error!);

        var inventoryResult = CheckInventoryResults(shouldSucceed);
        if (inventoryResult.IsFailure)
            return Result<int>.Failure(inventoryResult.Error!);

        var paymentResult = ProcessPaymentResults(shouldSucceed);
        if (paymentResult.IsFailure)
            return Result<int>.Failure(paymentResult.Error!);

        return Result<int>.Success(1);
    }

    private Result ValidateOrderResults(bool shouldSucceed)
    {
        if (!shouldSucceed)
            return Result.Failure("Order validation failed");
        return CallDepth1Results(shouldSucceed);
    }

    private Result CallDepth1Results(bool shouldSucceed)
    {
        return CallDepth2Results(shouldSucceed);
    }

    private Result CallDepth2Results(bool shouldSucceed)
    {
        return CallDepth3Results(shouldSucceed);
    }

    private Result CallDepth3Results(bool shouldSucceed)
    {
        return CallDepth4Results(shouldSucceed);
    }

    private Result CallDepth4Results(bool shouldSucceed)
    {
        return CallDepth5Results(shouldSucceed);
    }

    private Result CallDepth5Results(bool shouldSucceed)
    {
        if (!shouldSucceed)
            return Result.Failure(new BusinessRuleError("DEEP_ERROR", "Error at depth 5"));
        return Result.Success();
    }

    private Result CheckInventoryResults(bool shouldSucceed)
    {
        if (!shouldSucceed)
            return Result.Failure(new BusinessRuleError("INSUFFICIENT_STOCK", "Not enough stock"));
        return Result.Success();
    }

    private Result ProcessPaymentResults(bool shouldSucceed)
    {
        if (!shouldSucceed)
            return Result.Failure(
                new BusinessRuleError("PAYMENT_FAILED", "Payment processing failed")
            );
        return Result.Success();
    }

    // Helper methods for multiple validations
    private void ValidateWithExceptions(string username, string password, int age)
    {
        if (username.Length < 3)
            throw new ValidationException("username", "Username too short");

        if (password.Length < 8)
            throw new ValidationException("password", "Password too short");

        if (age < 18)
            throw new ValidationException("age", "Must be 18 or older");

        if (username.Contains("0"))
            throw new BusinessRuleException("INVALID_USERNAME", "Username cannot contain numbers");
    }

    private Result ValidateWithResults(string username, string password, int age)
    {
        if (username.Length < 3)
            return Result.Failure(Error.Validation("username", "Username too short"));

        if (password.Length < 8)
            return Result.Failure(Error.Validation("password", "Password too short"));

        if (age < 18)
            return Result.Failure(Error.Validation("age", "Must be 18 or older"));

        if (username.Contains("0"))
            return Result.Failure(
                new BusinessRuleError("INVALID_USERNAME", "Username cannot contain numbers")
            );

        return Result.Success();
    }
}

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class ErrorPropagationBenchmark
{
    private const int CallDepth = 10;
    private const int Iterations = 100;

    [Benchmark]
    public int Exception_ErrorPropagation()
    {
        int errors = 0;
        for (int i = 0; i < Iterations; i++)
        {
            try
            {
                PropagateExceptionThroughStack(0, CallDepth);
            }
            catch
            {
                errors++;
            }
        }
        return errors;
    }

    [Benchmark]
    public int Result_ErrorPropagation()
    {
        int errors = 0;
        for (int i = 0; i < Iterations; i++)
        {
            var result = PropagateResultThroughStack(0, CallDepth);
            if (result.IsFailure)
                errors++;
        }
        return errors;
    }

    private void PropagateExceptionThroughStack(int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth)
            throw new InvalidOperationException($"Error at depth {currentDepth}");

        PropagateExceptionThroughStack(currentDepth + 1, maxDepth);
    }

    private Result PropagateResultThroughStack(int currentDepth, int maxDepth)
    {
        if (currentDepth >= maxDepth)
            return Result.Failure($"Error at depth {currentDepth}");

        return PropagateResultThroughStack(currentDepth + 1, maxDepth);
    }
}
