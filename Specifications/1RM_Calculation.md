# One Rep Maximum Calculation

There are 3 documented algorithms for determining a maximum weight a person can lift based on how much they can lift for a range of 3-10 repetitions. Any set that goes outside of this rep range should not be considered. Because of the variance between the 3 algorithms, we will use an average of all 3 to provide a 1RM. The final result should be rounded to 2 decimal places.

## Core Variables and Data Types

- W (Weight Lifted): Float. Must be greater than zero.
- R (Repetitions): Integer. Must be >= 3 and <= 10

## Epley Algorithm

This is the standard model used by most fitness applications due to its linear simplicity.

### Algorithmic Logic

- Divide the repetitions (R) by 30.0
- Add 1.0 to the result.
- Multiply the sum by the weight (W)

```python
def calculate_epley(weight: float, reps: int) -> float:
    if reps == 1:
        return weight
    return weight * (1 + (reps / 30.0))
```


## Brzycki Algorithm

This formula operates on a predictable inverse-linear curve. It is highly accurate for low-repetition ranges but introduces a critical bug vulnerability if reps equal 37.

### Algorithm Logic

- Subtract repetitions (R) from 37
- Divide 36.0 by that result
- Multiply the final quotient by the weight (W)

```python
def calculate_brzycki(weight: float, reps: int) -> float:
    if reps == 1:
        return weight
    if reps >= 37:
        raise ValueError("Reps must be less than 37 to avoid division by zero or negative outputs.")
    return weight * (36.0 / (37 - reps))
```

## Lombardi Algorithm

Unlike Epley or Brzycki, Lombardi uses an exponential curve. It is optimized for powerlifting movements and does not penalize higher rep ranges quite as severely.

### Algorithm Logic

- Raise the repetitions (R) to the power of 0.10
- Multiply that result by the weight (W)

```python
import math

def calculate_lombardi(weight: float, reps: int) -> float:
    if reps == 1:
        return weight
    return weight * math.pow(reps, 0.10)
```
