import { useFormik } from "formik";
import * as Yup from "yup";
import FormikInput from "@/FormikInput.jsx";

const AIInputForm = ({ onSubmit }) => {
  const formik = useFormik({
    initialValues: {
      name: "",
      age: 0,
    },
    validationSchema: Yup.object({
      name: Yup.string()
        .required("Name is required")
        .test("test-string", 'String must be "correct"', function (value) {
          if (value !== "correct") {
            return false;
          }
          return true;
        }),
      age: Yup.number()
        .min(1, "Age must be at least 1")
        .required("Age is required"),
    }),
    onSubmit: (values) => {
      onSubmit(values);
    },
  });

  return (
    <form
      onSubmit={formik.handleSubmit}
      className="bg-red-50 rounded-lg p-8 flex flex-col mb-2 gap-2"
    >
      <h2 className="font-bold text-xl mb-4">Formik input, AI validation</h2>
      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          field="name"
          label="Name"
          vertical={true}
        />
        <div className="flex flex-row bg-red-200 rounded flex-grow p-1 gap-1">
          <textarea className="flex-grow">nth</textarea>
          <button className="bg-red-300 rounded px-2" type="button">
            Set Rule
          </button>
        </div>
      </div>

      <div className="flex flex-row gap-2">
        <FormikInput
          formik={formik}
          type="number"
          field="age"
          label="Age"
          vertical={true}
        />
        <div className="flex flex-row bg-red-200 rounded flex-grow p-1 gap-1">
          <textarea className="flex-grow">nth</textarea>
          <button className="bg-red-300 rounded px-2" type="button">
            Set Rule
          </button>
        </div>
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
