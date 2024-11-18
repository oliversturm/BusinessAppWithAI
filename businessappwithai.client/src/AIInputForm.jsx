import { useFormik } from "formik";
import * as Yup from "yup";
import FormikInput from "@/FormikInput.jsx";
import { useState } from "react";
import RuleEditor from "@/RuleEditor.jsx";

const valueHandler = (value) => {
  if (typeof value === "number") return String(value);
  else if (typeof value === "string") return value;
  else {
    // This may fail for some types, we only handle the ones
    // needed for this demo.
    return JSON.stringify(value);
  }
};

const validate = (field, value, context) =>
  fetch("http://localhost:5086/api/validate", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      field,
      value: valueHandler(value),
    }),
  })
    .then((response) => response.json())
    .then((result) => {
      if (result.valid) {
        return true;
      } else {
        return context.createError({
          message: result.message,
        });
      }
    });

const AIInputForm = ({ onSubmit }) => {
  const formik = useFormik({
    initialValues: {
      name: "",
      age: 0,
    },
    validationSchema: Yup.object({
      name: Yup.string().test("name-language-rule", function (value, context) {
        return validate("name", value, context);
      }),
      age: Yup.number().test("age-language-rule", function (value, context) {
        return validate("age", value, context);
      }),
      email: Yup.string().test(
        "email-language-rule",
        function (value, context) {
          return validate("email", value, context);
        },
      ),
    }).test("entity-language-rule", function (value, context) {
      return validate("_entity", value, context);
    }),
    onSubmit: (values) => {
      onSubmit(values);
    },
  });

  const [rules, setRules] = useState({
    _entity: "",
    name: "",
    age: "",
    email: "",
  });
  const ruleChanged = (field) => (e) => {
    setRules((r) => ({ ...r, [field]: e.target.value }));
  };

  const configureRule = (field, rule) =>
    fetch("http://localhost:5086/api/configureRule", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        field,
        ruleText: rule,
      }),
    });

  return (
    <form
      onSubmit={formik.handleSubmit}
      className="bg-red-50 rounded-lg p-8 flex flex-col mb-2 gap-2"
    >
      <div className="flex flex-row gap-2 mb-4">
        <h2 className="font-bold text-xl mb-4">Formik input, AI validation</h2>
        <RuleEditor
          name="_entity"
          value={rules._entity}
          onChange={ruleChanged}
          configureRule={configureRule}
        />
      </div>
      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          field="name"
          label="Name"
          vertical={true}
        />
        <RuleEditor
          name="name"
          value={rules.name}
          onChange={ruleChanged}
          configureRule={configureRule}
        />
      </div>

      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          type="number"
          field="age"
          label="Age"
          vertical={true}
        />
        <RuleEditor
          name="age"
          value={rules.age}
          onChange={ruleChanged}
          configureRule={configureRule}
        />
      </div>

      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          field="email"
          label="Email"
          vertical={true}
        />
        <RuleEditor
          name="email"
          value={rules.email}
          onChange={ruleChanged}
          configureRule={configureRule}
        />
      </div>

      <button
        type="submit"
        className="ml-auto bg-green-600 text-white font-bold px-4 py-2 rounded"
      >
        Submit
      </button>
    </form>
  );
};

export default AIInputForm;
