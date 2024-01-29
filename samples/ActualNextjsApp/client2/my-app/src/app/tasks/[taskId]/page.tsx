   export function generateStaticParams() {
     return [{ taskId: '[taskId]' }]
   }
 
   export default function TaskPage({ params }: { params: { taskId: string } }) {
     const { taskId } = params;
     return (
       <div>
         Some task: {taskId}
       </div>
     );
   }
