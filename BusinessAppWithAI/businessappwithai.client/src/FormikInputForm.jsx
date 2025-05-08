import { useFormik } from "formik";
import * as Yup from "yup";
import FormikInput from "@/FormikInput.jsx";

const FormikInputForm = ({ onSubmit }) => {
  const formik = useFormik({
    initialValues: {
      name: "",
      age: 0,
      email: "",
    },
    validationSchema: Yup.object({
      name: Yup.string().required("Name is required"),
      age: Yup.number()
        .min(1, "Age must be at least 1")
        .required("Age is required"),
      email: Yup.string()
        .email("Email must be a valid email address")
        .test(
          "valid-domain",
          "Email domain must be neogeeks.de or oliversturm.com",
          (value) => {
            if (!value) return false;
            const domain = value.split("@")[1];
            return ["neogeeks.de", "oliversturm.com"].includes(domain);
          },
        )
        .required(),
    }),
    onSubmit: (values) => {
      onSubmit(values);
    },
  });

  return (
    <form
      onSubmit={formik.handleSubmit}
      className="bg-blue-50 rounded-lg p-8 flex flex-col mb-2 gap-2"
    >
      <h2 className="font-bold text-xl mb-4">Formik input, yup validation</h2>
      <FormikInput formik={formik} field="name" label="Name" />

      <FormikInput formik={formik} type="number" field="age" label="Age" />

      <FormikInput formik={formik} field="email" label="Email" />

      <button
        type="submit"
        className="ml-auto bg-green-600 text-white font-bold px-4 py-2 rounded"
      >
        Submit
      </button>
    </form>
  );
};

export default FormikInputForm;
