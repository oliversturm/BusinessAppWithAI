(function() {
    function validateName(name) {
        if (!name) return "Name is required.";
        if (name.length < 3) return "Name must be at least three characters long.";
        return undefined;
    }

    function validateAge(age) {
        if (age < 1 || age > 120) return "Age must be between 1 and 120.";
        return undefined;
    }

    function validateEmail(email) {
        if (!email) return "Email is required.";
        var emailPattern = /^[^@\s]+@((oliversturm\.com)|(neogeeks\.de))$/;
        if (!emailPattern.test(email)) return "Email must be a valid address at oliversturm.com or neogeeks.de.";
        return undefined;
    }

    function validateEntity(entity) {
        var errors = [];

        var nameError = validateName(entity.name);
        if (nameError) errors.push(nameError);

        var ageError = validateAge(entity.age);
        if (ageError) errors.push(ageError);

        var emailError = validateEmail(entity.email);
        if (emailError) errors.push(emailError);

        if (entity.age < 70 && (entity.name === "Percival" || entity.name === "Ethelreda")) {
            errors.push("People under 70 must not be named Percival or Ethelreda.");
        }

        return errors.length > 0 ? errors : undefined;
    }

    return {
        validateName: validateName,
        validateAge: validateAge,
        validateEmail: validateEmail,
        validateEntity: validateEntity
    };
})();