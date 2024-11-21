const ErrorDisplay = ({ formik, field }) => {
  return (
    <>
      {formik.errors[field] ? (
        <div className="ml-2 text-xs font-bold text-red-600">
          {formik.errors[field]}
        </div>
      ) : null}
    </>
  );
};

export default ErrorDisplay;
