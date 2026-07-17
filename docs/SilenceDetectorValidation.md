# SilenceDetectorValidation

Provides validation utilities for silence detection parameters in the NAudio effects pipeline. This static class ensures that configuration values for silence detection meet required constraints before processing audio streams, preventing runtime errors due to invalid or out-of-range settings.

## API

### `public static IReadOnlyList<string> Validate(ISilenceDetectorSettings settings)`
Validates the provided silence detector settings and returns a list of error messages if any constraints are violated.

**Parameters:**
- `settings` (`ISilenceDetectorSettings`): The silence detector configuration to validate.

**Returns:**
- (`IReadOnlyList<string>`): A read-only list of error messages. If no errors are found, the list is empty.

**Throws:**
- `ArgumentNullException`: Thrown if `settings` is `null`.

---

### `public static bool IsValid(ISilenceDetectorSettings settings)`
Determines whether the provided silence detector settings are valid.

**Parameters:**
- `settings` (`ISilenceDetectorSettings`): The silence detector configuration to validate.

**Returns:**
- (`bool`): `true` if the settings are valid; otherwise, `false`.

**Throws:**
- `ArgumentNullException`: Thrown if `settings` is `null`.

---

### `public static void EnsureValid(ISilenceDetectorSettings settings)`
Ensures the provided silence detector settings are valid, throwing an exception if validation fails.

**Parameters:**
- `settings` (`ISilenceDetectorSettings`): The silence detector configuration to validate.

**Throws:**
- `ArgumentNullException`: Thrown if `settings` is `null`.
- `ArgumentException`: Thrown if the settings contain invalid values, with an error message detailing the validation failures.

---

### `public static IReadOnlyList<string> Validate(SilenceDetector detector)`
Validates the provided silence detector instance and returns a list of error messages if any constraints are violated.

**Parameters:**
- `detector` (`SilenceDetector`): The silence detector instance to validate.

**Returns:**
- (`IReadOnlyList<string>`): A read-only list of error messages. If no errors are found, the list is empty.

**Throws:**
- `ArgumentNullException`: Thrown if `detector` is `null`.

---

### `public static bool IsValid(SilenceDetector detector)`
Determines whether the provided silence detector instance is valid.

**Parameters:**
- `detector` (`SilenceDetector`): The silence detector instance to validate.

**Returns:**
- (`bool`): `true` if the detector is valid; otherwise, `false`.

**Throws:**
- `ArgumentNullException`: Thrown if `detector` is `null`.

---

### `public static void EnsureValid(SilenceDetector detector)`
Ensures the provided silence detector instance is valid, throwing an exception if validation fails.

**Parameters:**
- `detector` (`SilenceDetector`): The silence detector instance to validate.

**Throws:**
- `ArgumentNullException`: Thrown if `detector` is `null`.
- `ArgumentException`: Thrown if the detector contains invalid values, with an error message detailing the validation failures.

## Usage

### Example 1: Validating SilenceDetectorSettings
