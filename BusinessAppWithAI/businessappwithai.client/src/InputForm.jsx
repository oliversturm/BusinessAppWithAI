import Input from "@/Input.jsx";
import { useCallback, useState } from "react";

const validateName = (name, setError) => {
  if (name.length < 3) {
    setError("Name must be at least 3 characters long");
    return false;
  } else {
    setError("");
    return true;
  }
};

const validateAge = (age, setError) => {
  if (/^[-+]?\d+$/.test(age) === false) {
    setError("Age must be a whole number");
    return false;
  } else if (age < 1) {
    setError("Age must be at least 1");
    return false;
  } else {
    setError("");
    return true;
  }
};

const validateEmail = (email, setError) => {
  if (/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) === false) {
    setError("Email must be a valid email address");
    return false;
  } else {
    setError("");
    return true;
  }
};

const InputForm = ({ onSubmit }) => {
  const [data, setData] = useState({ name: "", age: 0, email: "" });
  const [errors, setErrors] = useState({});
  const setError = (field) => (error) =>
    setErrors((es) => ({ ...es, [field]: error }));

  const nameChanged = (name) => {
    validateName(name, setError("name"));
    setData((d) => ({ ...d, name }));
  };
  const ageChanged = (age) => {
    validateAge(age, setError("age"));
    setData((d) => ({ ...d, age }));
  };
  const emailChanged = (email) => {
    validateEmail(email, setError("email"));
    setData((d) => ({ ...d, email }));
  };

  const innerSubmit = useCallback(
    (e) => {
      e.preventDefault();
      if (
        validateName(data.name, setError("name")) &&
        validateAge(data.age, setError("age")) &&
        validateEmail(data.email, setError("email"))
      ) {
        onSubmit(data);
        // Could also trigger standard submit if necessary
        // e.target.submit();
      }
    },
    [data],
  );

  return (
    <form
      onSubmit={innerSubmit}
      className="bg-amber-50 rounded-lg p-8 flex flex-col mb-2 gap-2"
    >
      <h2 className="font-bold text-xl mb-4">
        Manual input, manual validation
      </h2>
      <Input
        field="name"
        label="Name"
        value={data.name}
        onChange={nameChanged}
        error={errors.name}
      />
      <Input
        field="age"
        label="Age"
        value={data.age}
        onChange={ageChanged}
        error={errors.age}
      />
      <Input
        field="email"
        label="Email"
        value={data.email}
        onChange={emailChanged}
        error={errors.email}
      />

      <button
        type="submit"
        className="ml-auto bg-green-600 text-white font-bold px-4 py-2 rounded"
      >
        Submit
      </button>
    </form>
  );
};

export default InputForm;
