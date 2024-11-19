const ErrorDisplay = ({ formik, field }) => {
  //console.log(formik);
  return (
    <>
      {(field === "_entity" || formik.touched[field]) &&
      formik.errors[field] ? (
        <div className="ml-2 text-xs font-bold text-red-600">
          {formik.errors[field]}
        </div>
      ) : null}
    </>
  );
};

export default ErrorDisplay;
