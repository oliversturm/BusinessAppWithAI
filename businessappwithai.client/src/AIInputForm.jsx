import { useFormik } from "formik";
import * as Yup from "yup";
import FormikInput from "@/FormikInput.jsx";
import { useState } from "react";
import RuleEditor from "@/RuleEditor.jsx";
import ErrorDisplay from "@/ErrorDisplay.jsx";
import { ValidationError } from "yup";

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
        // the object-level _entity error is not included in the errors
        // collection by default because its context doesn't have a path
        // -- so we create our own error instead and pass the name _entity
        // so we can access the error for the UI
        const error =
          field === "_entity"
            ? new ValidationError(
                result.message,
                value,
                "_entity",
                undefined,
                true,
              )
            : context.createError({
                message: result.message,
              });

        return error;
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
    _entity:
      "Menschen unter 70 dürfen nicht Wunibald oder Frideruna heißen, das verstößt gegen Regeln des guten Geschmacks.",
    name: "Mindestens drei Zeichen!",
    age: "Mindestens 1, maximal 120",
    email:
      "Muss eine gültige E-Mail-Adresse sein, entweder @neogeeks.de oder @oliversturm.com",
  });
  const ruleChanged = (field) => (e) => {
    setRules((r) => ({ ...r, [field]: e.target.value }));
  };

  const [configuringRules, setConfiguringRules] = useState(false);
  const configureRules = () => {
    setConfiguringRules(true);
    return fetch("http://localhost:5086/api/configureRules", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(
        Object.keys(rules).map((k) => ({ field: k, ruleText: rules[k] })),
      ),
    }).then(() => {
      setConfiguringRules(false);
      formik.validateForm();
    });
  };

  return (
    <form
      onSubmit={formik.handleSubmit}
      className="bg-red-50 rounded-lg p-8 flex flex-col mb-2 gap-2"
    >
      <div className="flex flex-row gap-2 mb-4">
        <h2 className="font-bold text-xl mb-4">Formik input, AI validation</h2>
        <div className="flex flex-col flex-grow">
          <RuleEditor
            name="_entity"
            value={rules._entity}
            onChange={ruleChanged}
          />
          <ErrorDisplay formik={formik} field="_entity" />
        </div>
      </div>
      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          field="name"
          label="Name"
          vertical={true}
        />
        <RuleEditor name="name" value={rules.name} onChange={ruleChanged} />
      </div>

      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          type="number"
          field="age"
          label="Age"
          vertical={true}
        />
        <RuleEditor name="age" value={rules.age} onChange={ruleChanged} />
      </div>

      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          field="email"
          label="Email"
          vertical={true}
        />
        <RuleEditor name="email" value={rules.email} onChange={ruleChanged} />
      </div>

      <div className="ml-auto flex flex-row gap-2">
        <button
          className="bg-red-300 rounded px-2 disabled:bg-gray-300"
          type="button"
          onClick={() => configureRules()}
          disabled={configuringRules}
        >
          Set Rules
        </button>

        <button
          type="submit"
          className="bg-green-600 text-white font-bold px-4 py-2 rounded"
        >
          Submit
        </button>
      </div>
    </form>
  );
};

export default AIInputForm;
